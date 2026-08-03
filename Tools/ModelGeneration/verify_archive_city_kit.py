#!/usr/bin/env python3
"""Validate generated Archive City OBJ files without third-party packages."""

from __future__ import annotations

from pathlib import Path


EXPECTED = {
    "CentralMemoryArchive.obj": (50.0, 32.0, 42.0),
    "RuinedLowrise_A.obj": None,
    "RuinedMidrise_B.obj": None,
    "RuinedTower_C.obj": None,
    "MemoryStreetLamp_A.obj": None,
}


def parse_obj(path: Path) -> dict:
    vertices = []
    normals = []
    faces = []
    groups = set()
    materials = set()
    material_library = None

    for line_number, raw in enumerate(path.read_text(encoding="utf-8").splitlines(), start=1):
        line = raw.strip()
        if not line or line.startswith("#"):
            continue
        parts = line.split()
        keyword = parts[0]
        if keyword == "v":
            vertices.append(tuple(float(value) for value in parts[1:4]))
        elif keyword == "vn":
            normals.append(tuple(float(value) for value in parts[1:4]))
        elif keyword == "f":
            face = []
            for token in parts[1:]:
                indices = token.split("/")
                vertex_index = int(indices[0])
                normal_index = int(indices[2]) if len(indices) >= 3 and indices[2] else 0
                if vertex_index < 1 or vertex_index > len(vertices):
                    raise ValueError(f"{path.name}:{line_number}: invalid vertex index {vertex_index}")
                if normal_index < 1 or normal_index > len(normals):
                    raise ValueError(f"{path.name}:{line_number}: invalid normal index {normal_index}")
                face.append((vertex_index, normal_index))
            if len(face) < 3:
                raise ValueError(f"{path.name}:{line_number}: face has fewer than three vertices")
            faces.append(face)
        elif keyword == "g":
            groups.add(" ".join(parts[1:]))
        elif keyword == "usemtl":
            materials.add(" ".join(parts[1:]))
        elif keyword == "mtllib":
            material_library = " ".join(parts[1:])

    if not vertices or not faces:
        raise ValueError(f"{path.name}: empty mesh")
    if material_library is None or not (path.parent / material_library).is_file():
        raise ValueError(f"{path.name}: missing material library {material_library!r}")

    xs, ys, zs = zip(*vertices)
    size = (max(xs) - min(xs), max(zs) - min(zs), max(ys) - min(ys))
    triangles = sum(len(face) - 2 for face in faces)
    return {
        "vertices": len(vertices),
        "normals": len(normals),
        "faces": len(faces),
        "triangles": triangles,
        "groups": len(groups),
        "materials": len(materials),
        "size": size,
    }


def main() -> int:
    root = Path(__file__).resolve().parents[2]
    generated = root / "Assets" / "MemoryRecycler3D" / "Models" / "ArchiveCityKit" / "Generated"
    for filename, expected_size in EXPECTED.items():
        path = generated / filename
        if not path.is_file():
            raise FileNotFoundError(path)
        result = parse_obj(path)
        if result["triangles"] > 50_000:
            raise ValueError(f"{filename}: exceeds 50,000 triangle budget")
        if expected_size is not None:
            for actual, expected in zip(result["size"], expected_size):
                if abs(actual - expected) > 0.02:
                    raise ValueError(f"{filename}: size {result['size']} does not match {expected_size}")
        width, depth, height = result["size"]
        print(
            f"PASS {filename}: {result['triangles']:,} tris, {result['materials']} materials, "
            f"{result['groups']} groups, {width:.2f} x {depth:.2f} x {height:.2f}m"
        )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
