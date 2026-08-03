#!/usr/bin/env python3
"""Generate the Memory Recycler Archive City low/medium-poly model kit.

The script uses only the Python standard library and writes Wavefront OBJ/MTL
files that Unity can import directly.  Run with Blender's Python to also build
an editable .blend file and export FBX files (see --blender-export).

Coordinate system: Y up, one model unit equals one metre, origin at ground
centre, front facade points toward negative Z.
"""

from __future__ import annotations

import argparse
import math
import sys
from dataclasses import dataclass
from pathlib import Path
from typing import Iterable, Sequence


Vec3 = tuple[float, float, float]


MATERIALS = {
    "MR_Concrete": {"kd": (0.204, 0.220, 0.227), "ks": (0.08, 0.08, 0.08), "ns": 18},
    "MR_ConcreteDark": {"kd": (0.115, 0.130, 0.136), "ks": (0.05, 0.05, 0.05), "ns": 12},
    "MR_DarkMetal": {"kd": (0.086, 0.106, 0.114), "ks": (0.23, 0.25, 0.26), "ns": 48},
    "MR_SecondaryMetal": {"kd": (0.145, 0.173, 0.184), "ks": (0.18, 0.20, 0.21), "ns": 36},
    "MR_WindowDark": {"kd": (0.018, 0.030, 0.035), "ks": (0.18, 0.28, 0.30), "ns": 72},
    "MR_CyanEmission": {
        "kd": (0.035, 0.520, 0.540),
        "ke": (0.208, 0.902, 0.902),
        "ks": (0.30, 0.70, 0.70),
        "ns": 96,
    },
    "MR_DimCyan": {
        "kd": (0.035, 0.245, 0.260),
        "ke": (0.025, 0.180, 0.190),
        "ks": (0.18, 0.42, 0.44),
        "ns": 64,
    },
    "MR_Rust": {"kd": (0.408, 0.294, 0.231), "ks": (0.08, 0.07, 0.06), "ns": 12},
    "MR_Debris": {"kd": (0.155, 0.145, 0.135), "ks": (0.03, 0.03, 0.03), "ns": 8},
}


@dataclass
class Face:
    indices: tuple[int, ...]
    material: str
    group: str


class MeshBuilder:
    def __init__(self, name: str) -> None:
        self.name = name
        self.vertices: list[Vec3] = []
        self.faces: list[Face] = []

    def _add_vertices(self, values: Iterable[Vec3]) -> list[int]:
        start = len(self.vertices) + 1
        values = list(values)
        self.vertices.extend(values)
        return list(range(start, start + len(values)))

    def _face(self, indices: Sequence[int], material: str, group: str) -> None:
        self.faces.append(Face(tuple(indices), material, group))

    def box(
        self,
        name: str,
        center: Vec3,
        size: Vec3,
        material: str,
        rotation_y: float = 0.0,
    ) -> None:
        cx, cy, cz = center
        hx, hy, hz = (size[0] * 0.5, size[1] * 0.5, size[2] * 0.5)
        local = [
            (-hx, -hy, -hz), (hx, -hy, -hz), (hx, -hy, hz), (-hx, -hy, hz),
            (-hx, hy, -hz), (hx, hy, -hz), (hx, hy, hz), (-hx, hy, hz),
        ]
        angle = math.radians(rotation_y)
        ca, sa = math.cos(angle), math.sin(angle)
        verts = []
        for x, y, z in local:
            verts.append((cx + x * ca + z * sa, cy + y, cz - x * sa + z * ca))
        i = self._add_vertices(verts)
        for face in (
            (i[0], i[1], i[2], i[3]),
            (i[4], i[7], i[6], i[5]),
            (i[0], i[4], i[5], i[1]),
            (i[3], i[2], i[6], i[7]),
            (i[0], i[3], i[7], i[4]),
            (i[1], i[5], i[6], i[2]),
        ):
            self._face(face, material, name)

    def tube(
        self,
        name: str,
        start: Vec3,
        end: Vec3,
        radius: float,
        material: str,
        sides: int = 10,
        cap: bool = True,
    ) -> None:
        sx, sy, sz = start
        ex, ey, ez = end
        wx, wy, wz = ex - sx, ey - sy, ez - sz
        length = math.sqrt(wx * wx + wy * wy + wz * wz)
        if length <= 1e-6:
            return
        wx, wy, wz = wx / length, wy / length, wz / length
        helper = (0.0, 1.0, 0.0) if abs(wy) < 0.9 else (1.0, 0.0, 0.0)
        ux = wy * helper[2] - wz * helper[1]
        uy = wz * helper[0] - wx * helper[2]
        uz = wx * helper[1] - wy * helper[0]
        ul = math.sqrt(ux * ux + uy * uy + uz * uz)
        ux, uy, uz = ux / ul, uy / ul, uz / ul
        vx = wy * uz - wz * uy
        vy = wz * ux - wx * uz
        vz = wx * uy - wy * ux
        verts: list[Vec3] = []
        for px, py, pz in (start, end):
            for n in range(sides):
                a = math.tau * n / sides
                c, s = math.cos(a) * radius, math.sin(a) * radius
                verts.append((px + ux * c + vx * s, py + uy * c + vy * s, pz + uz * c + vz * s))
        ids = self._add_vertices(verts)
        for n in range(sides):
            nn = (n + 1) % sides
            self._face((ids[n], ids[nn], ids[sides + nn], ids[sides + n]), material, name)
        if cap:
            self._face(tuple(reversed(ids[:sides])), material, name)
            self._face(tuple(ids[sides:]), material, name)

    def torus_xy(
        self,
        name: str,
        center: Vec3,
        major_radius: float,
        minor_radius: float,
        material: str,
        major_segments: int = 32,
        minor_segments: int = 8,
    ) -> None:
        cx, cy, cz = center
        verts: list[Vec3] = []
        for u_index in range(major_segments):
            u = math.tau * u_index / major_segments
            cu, su = math.cos(u), math.sin(u)
            for v_index in range(minor_segments):
                v = math.tau * v_index / minor_segments
                ring = major_radius + minor_radius * math.cos(v)
                verts.append((cx + ring * cu, cy + ring * su, cz + minor_radius * math.sin(v)))
        ids = self._add_vertices(verts)
        for u_index in range(major_segments):
            un = (u_index + 1) % major_segments
            for v_index in range(minor_segments):
                vn = (v_index + 1) % minor_segments
                a = ids[u_index * minor_segments + v_index]
                b = ids[un * minor_segments + v_index]
                c = ids[un * minor_segments + vn]
                d = ids[u_index * minor_segments + vn]
                self._face((a, b, c, d), material, name)

    @property
    def triangle_count(self) -> int:
        return sum(max(0, len(face.indices) - 2) for face in self.faces)

    @property
    def bounds(self) -> tuple[Vec3, Vec3]:
        xs, ys, zs = zip(*self.vertices)
        return (min(xs), min(ys), min(zs)), (max(xs), max(ys), max(zs))

    def write_obj(self, path: Path, material_file: str) -> None:
        path.parent.mkdir(parents=True, exist_ok=True)
        with path.open("w", encoding="utf-8", newline="\n") as handle:
            handle.write("# Memory Recycler 3D - generated game-ready model\n")
            handle.write(f"# 1 unit = 1 metre | triangles = {self.triangle_count}\n")
            handle.write(f"mtllib {material_file}\n")
            handle.write(f"o {self.name}\n")
            for x, y, z in self.vertices:
                handle.write(f"v {x:.6f} {y:.6f} {z:.6f}\n")
            face_normals: list[Vec3] = []
            for face in self.faces:
                a = self.vertices[face.indices[0] - 1]
                b = self.vertices[face.indices[1] - 1]
                c = self.vertices[face.indices[2] - 1]
                edge_a = (b[0] - a[0], b[1] - a[1], b[2] - a[2])
                edge_b = (c[0] - a[0], c[1] - a[1], c[2] - a[2])
                face_normals.append(_normalize(_cross(edge_a, edge_b)))
            for x, y, z in face_normals:
                handle.write(f"vn {x:.6f} {y:.6f} {z:.6f}\n")
            handle.write("s off\n")
            previous_group = previous_material = None
            for normal_index, face in enumerate(self.faces, start=1):
                export_group = category_name(face.group)
                if export_group != previous_group:
                    handle.write(f"g {export_group}\n")
                    previous_group = export_group
                if face.material != previous_material:
                    handle.write(f"usemtl {face.material}\n")
                    previous_material = face.material
                handle.write("f " + " ".join(f"{i}//{normal_index}" for i in face.indices) + "\n")


def windows_front(
    mesh: MeshBuilder,
    prefix: str,
    xs: Sequence[float],
    ys: Sequence[float],
    z: float,
    width: float = 0.56,
    height: float = 1.65,
    cyan_every: int = 7,
) -> None:
    index = 0
    for y in ys:
        for x in xs:
            material = "MR_DimCyan" if cyan_every and index % cyan_every == 0 else "MR_WindowDark"
            mesh.box(f"{prefix}_Window_{index:02d}", (x, y, z), (width, height, 0.10), material)
            index += 1


def category_name(part_name: str) -> str:
    """Collapse descriptive part names into Unity-friendly model groups."""
    prefixes = (
        "MainTower", "LeftWing", "RightWing", "Entrance", "ArchiveSymbol",
        "Windows", "CyanLights", "Pipes", "ExteriorPanels", "DamageParts",
        "Damage", "Debris", "Architecture", "Facade", "Props", "Lamp",
    )
    for prefix in prefixes:
        if part_name == prefix or part_name.startswith(prefix + "_"):
            return prefix
    return part_name.split("_", 1)[0]


def facade_ribs(mesh: MeshBuilder, prefix: str, xs: Sequence[float], y: float, height: float, z: float) -> None:
    for index, x in enumerate(xs):
        mesh.box(f"{prefix}_Rib_{index:02d}", (x, y, z), (0.46, height, 0.48), "MR_SecondaryMetal")


def debris_cluster(mesh: MeshBuilder, prefix: str, x: float, z: float, count: int, scale: float = 1.0) -> None:
    for i in range(count):
        ox = ((i * 37) % 11 - 5) * 0.17 * scale
        oz = ((i * 23) % 9 - 4) * 0.15 * scale
        sx = (0.18 + (i % 4) * 0.10) * scale
        sy = (0.10 + (i % 3) * 0.08) * scale
        sz = (0.16 + ((i + 2) % 5) * 0.08) * scale
        mesh.box(
            f"{prefix}_Debris_{i:02d}",
            (x + ox, sy * 0.5, z + oz),
            (sx, sy, sz),
            "MR_Debris" if i % 4 else "MR_Rust",
            rotation_y=(i * 29) % 90,
        )


def build_archive() -> MeshBuilder:
    m = MeshBuilder("CentralMemoryArchive")
    # 50m x 32m base and readable front plaza connection.
    m.box("Architecture_Base", (0, 0.35, 0), (50, 0.70, 32), "MR_ConcreteDark")
    m.box("Architecture_FrontPlinth", (0, 0.75, -12.8), (42, 0.80, 5.6), "MR_Concrete")

    # Main tower: 18m x 25m x 42m, with stepped Art-Deco silhouette.
    m.box("MainTower_Core", (0, 18.0, 1.5), (18, 36, 25), "MR_Concrete")
    m.box("MainTower_Upper", (0, 37.0, 2.0), (13.5, 10, 20), "MR_Concrete")
    m.box("MainTower_Crown", (0, 41.0, 2.2), (10.5, 2, 17), "MR_ConcreteDark")
    m.box("MainTower_CrownStep_L", (-5.5, 39.8, 1.7), (2.2, 3.6, 18), "MR_ConcreteDark")
    m.box("MainTower_CrownStep_R", (5.5, 40.1, 2.0), (2.2, 3.0, 17), "MR_ConcreteDark")

    # Symmetrical lower wings, each roughly 16m wide and 22m high.
    m.box("LeftWing_Main", (-17.0, 11.0, 1.0), (16, 22, 24), "MR_Concrete")
    m.box("RightWing_Main", (17.0, 11.0, 1.0), (16, 22, 24), "MR_Concrete")
    m.box("LeftWing_OuterStep", (-23.0, 8.5, 2.0), (4, 17, 20), "MR_ConcreteDark")
    m.box("RightWing_OuterStep", (23.0, 8.5, 2.0), (4, 17, 20), "MR_ConcreteDark")
    m.box("LeftWing_UpperStep", (-12.0, 14.0, 1.5), (6, 6, 21), "MR_ConcreteDark")
    m.box("RightWing_UpperStep", (12.0, 14.0, 1.5), (6, 6, 21), "MR_ConcreteDark")

    # Deep recessed 7m x 6m main entrance and simple visible lobby.
    m.box("Entrance_FrameTop", (0, 7.2, -11.55), (10.5, 2.4, 2.2), "MR_ConcreteDark")
    m.box("Entrance_FrameLeft", (-4.55, 3.3, -11.55), (1.4, 6.6, 2.2), "MR_ConcreteDark")
    m.box("Entrance_FrameRight", (4.55, 3.3, -11.55), (1.4, 6.6, 2.2), "MR_ConcreteDark")
    m.box("Entrance_Recess", (0, 3.0, -10.85), (7.0, 6.0, 0.35), "MR_WindowDark")
    m.box("Entrance_SecurityDoor_L", (-1.78, 2.9, -11.08), (3.35, 5.5, 0.18), "MR_DarkMetal")
    m.box("Entrance_SecurityDoor_R", (1.78, 2.9, -11.08), (3.35, 5.5, 0.18), "MR_DarkMetal")
    m.box("Entrance_CyanHeader", (0, 6.25, -11.82), (7.2, 0.18, 0.18), "MR_CyanEmission")
    m.box("Entrance_SignPanel", (0, 8.15, -11.82), (9.2, 1.35, 0.22), "MR_DarkMetal")
    m.box("Entrance_LobbyFloor", (0, 0.55, -7.0), (7.0, 0.25, 8.0), "MR_SecondaryMetal")
    m.box("Entrance_LobbyTerminal", (0, 1.45, -5.1), (1.6, 2.0, 1.1), "MR_DarkMetal")
    m.box("Entrance_LobbyTerminalGlow", (0, 1.65, -5.68), (0.9, 1.0, 0.08), "MR_CyanEmission")

    # Wide approach stairs: kept simple for separate box colliders later.
    for i in range(5):
        m.box(
            f"Entrance_Step_{i:02d}",
            (0, 0.10 + i * 0.14, -15.55 + i * 0.72),
            (18 - i * 1.1, 0.20, 0.82),
            "MR_Concrete",
        )

    # Strong vertical facade rhythm.
    facade_ribs(m, "MainTower", (-8.3, -6.8, -5.1, 5.1, 6.8, 8.3), 19.5, 37.5, -11.18)
    facade_ribs(m, "LeftWing", (-24.1, -21.4, -18.7, -16.0, -13.3, -10.6), 10.7, 20.8, -11.18)
    facade_ribs(m, "RightWing", (10.6, 13.3, 16.0, 18.7, 21.4, 24.1), 10.7, 20.8, -11.18)

    # Narrow windows with a small percentage of dim cyan panes.
    windows_front(m, "MainTower", (-7.2, -5.8, 5.8, 7.2), (11, 14, 17, 20, 23, 26, 29, 32), -11.31, 0.62, 1.8, 9)
    windows_front(m, "LeftWing", (-22.8, -20.1, -17.4, -14.7, -12.0), (3.6, 6.7, 9.8, 12.9, 16.0, 19.1), -11.31, 0.58, 1.55, 11)
    windows_front(m, "RightWing", (12.0, 14.7, 17.4, 20.1, 22.8), (3.6, 6.7, 9.8, 12.9, 16.0, 19.1), -11.31, 0.58, 1.55, 13)

    # Central cyan memory-energy spine and Art-Deco bands.
    m.box("CyanLights_MainSpine", (0, 23.0, -11.42), (0.55, 27.0, 0.24), "MR_CyanEmission")
    m.box("CyanLights_SpineLeft", (-1.15, 22.0, -11.38), (0.15, 23.0, 0.18), "MR_DimCyan")
    m.box("CyanLights_SpineRight", (1.15, 22.0, -11.38), (0.15, 23.0, 0.18), "MR_DimCyan")
    for y, width in ((11.0, 5.0), (16.0, 6.2), (27.0, 5.6), (33.0, 4.8)):
        m.box(f"ExteriorPanels_TowerBand_{int(y)}", (0, y, -11.29), (width, 0.34, 0.35), "MR_DarkMetal")

    # Large archive emblem, visible from a long distance.
    m.torus_xy("ArchiveSymbol_OuterRing", (0, 34.6, -11.65), 3.35, 0.27, "MR_CyanEmission", 40, 8)
    m.torus_xy("ArchiveSymbol_InnerRing", (0, 34.6, -11.67), 2.35, 0.14, "MR_DimCyan", 32, 6)
    m.tube("ArchiveSymbol_Vertical", (0, 30.4, -11.68), (0, 38.8, -11.68), 0.20, "MR_CyanEmission", 10)
    m.tube("ArchiveSymbol_NodeLeft", (-2.2, 34.6, -11.68), (-0.65, 34.6, -11.68), 0.12, "MR_CyanEmission", 8)
    m.tube("ArchiveSymbol_NodeRight", (0.65, 34.6, -11.68), (2.2, 34.6, -11.68), 0.12, "MR_CyanEmission", 8)

    # Structural beams, maintenance panels, vents and pipes.
    for side in (-1, 1):
        sx = side * 9.2
        m.box(f"ExteriorPanels_TowerButtress_{side}", (sx, 14.0, -8.8), (1.2, 28, 4.8), "MR_ConcreteDark", rotation_y=side * 4)
        m.tube(f"Pipes_TowerMain_{side}", (side * 7.8, 2.0, -11.72), (side * 7.8, 17.5, -11.72), 0.16, "MR_Rust", 10)
        m.tube(f"Pipes_TowerElbow_{side}", (side * 7.8, 17.5, -11.72), (side * 6.7, 18.6, -11.72), 0.16, "MR_Rust", 10)
        for x in (side * 13.0, side * 18.0, side * 23.0):
            m.box(f"ExteriorPanels_Maintenance_{x:+.0f}", (x, 4.8, -11.48), (1.5, 2.1, 0.24), "MR_SecondaryMetal")
            m.box(f"ExteriorPanels_Vent_{x:+.0f}", (x, 15.3, -11.48), (1.7, 0.95, 0.28), "MR_DarkMetal")
            for v in range(3):
                m.box(f"ExteriorPanels_VentSlat_{x:+.0f}_{v}", (x, 15.0 + v * 0.30, -11.66), (1.35, 0.08, 0.08), "MR_SecondaryMetal")

    # Moderate 20-30% visual damage without destroying landmark readability.
    m.box("DamageParts_CrownMissingPanel_L", (-5.9, 40.95, -7.7), (1.2, 2.1, 2.5), "MR_DarkMetal", -8)
    m.box("DamageParts_CrownMissingPanel_R", (4.9, 41.3, -6.8), (1.0, 1.4, 3.0), "MR_Rust", 12)
    m.tube("DamageParts_ExposedRebar_L", (-6.0, 40.6, -7.2), (-6.2, 41.9, -7.0), 0.07, "MR_Rust", 8)
    m.tube("DamageParts_ExposedRebar_R", (5.0, 40.8, -6.5), (5.2, 41.9, -6.2), 0.07, "MR_Rust", 8)
    m.box("DamageParts_LeftWingTiltedPanel", (-20.0, 18.0, -11.75), (2.4, 3.8, 0.20), "MR_ConcreteDark", 8)
    m.box("DamageParts_RightWingMissingPanel", (15.5, 12.0, -11.62), (2.1, 3.1, 0.16), "MR_Rust", -6)
    debris_cluster(m, "Debris_LeftEntrance", -6.2, -14.0, 14, 1.3)
    debris_cluster(m, "Debris_RightWing", 18.0, -12.8, 12, 1.1)
    return m


def build_lowrise() -> MeshBuilder:
    m = MeshBuilder("RuinedLowrise_A")
    m.box("Architecture_Main", (0, 4.5, 0), (12, 9, 9), "MR_Concrete")
    m.box("Architecture_RoofStep", (-1.0, 9.5, 0.5), (8.5, 1.0, 7.0), "MR_ConcreteDark")
    m.box("Architecture_SideAnnex", (5.5, 2.8, 1.0), (3.0, 5.6, 6.0), "MR_ConcreteDark")
    facade_ribs(m, "Facade", (-5.4, -3.0, -0.8, 1.4, 3.6, 5.4), 4.7, 8.4, -4.68)
    windows_front(m, "Windows", (-4.2, -1.9, 0.4, 2.7, 4.7), (3.0, 6.3), -4.62, 0.72, 1.55, 6)
    m.box("Entrance_Recess", (-3.7, 1.7, -4.65), (2.2, 3.4, 0.24), "MR_WindowDark")
    m.box("CyanLights_Door", (-3.7, 3.5, -4.8), (2.4, 0.14, 0.14), "MR_CyanEmission")
    m.tube("Pipes_Front", (5.1, 0.5, -4.82), (5.1, 7.8, -4.82), 0.11, "MR_Rust", 8)
    m.box("Props_RoofUnit", (1.8, 10.3, 1.0), (2.4, 1.1, 1.8), "MR_SecondaryMetal")
    m.box("Damage_RoofPanel", (-4.7, 9.9, -2.4), (2.5, 0.35, 2.0), "MR_Rust", 13)
    debris_cluster(m, "Debris", 3.5, -5.1, 10, 0.8)
    return m


def build_midrise() -> MeshBuilder:
    m = MeshBuilder("RuinedMidrise_B")
    m.box("Architecture_Main", (0, 9.0, 0), (10, 18, 8), "MR_Concrete")
    m.box("Architecture_RearStep", (0, 13.0, 2.3), (8, 9, 4.5), "MR_ConcreteDark")
    m.box("Architecture_RoofCrown", (-0.5, 18.8, 0.3), (7.5, 1.6, 6.4), "MR_ConcreteDark")
    facade_ribs(m, "Facade", (-4.5, -2.6, -0.9, 0.9, 2.6, 4.5), 9.0, 17.4, -4.25)
    windows_front(m, "Windows", (-3.7, -1.8, 0, 1.8, 3.7), (3, 6, 9, 12, 15), -4.30, 0.55, 1.35, 8)
    m.box("Entrance", (0, 2.0, -4.33), (2.7, 4.0, 0.24), "MR_WindowDark")
    m.box("CyanLights_Sign", (0, 4.4, -4.48), (4.1, 0.22, 0.18), "MR_CyanEmission")
    m.tube("Pipes_Left", (-4.2, 0.5, -4.55), (-4.2, 12.5, -4.55), 0.10, "MR_Rust", 8)
    m.tube("Pipes_Top", (-4.2, 12.5, -4.55), (-2.6, 14.0, -4.55), 0.10, "MR_Rust", 8)
    m.box("Damage_TiltedPanel", (3.0, 16.2, -4.52), (2.0, 2.6, 0.18), "MR_Rust", -9)
    debris_cluster(m, "Debris", -3.8, -4.7, 10, 0.75)
    return m


def build_tower() -> MeshBuilder:
    m = MeshBuilder("RuinedTower_C")
    m.box("Architecture_Lower", (0, 8.0, 0), (12, 16, 10), "MR_Concrete")
    m.box("Architecture_Middle", (0, 19.0, 0.5), (9.5, 12, 8.5), "MR_Concrete")
    m.box("Architecture_Upper", (0, 28.0, 0.8), (7.0, 8, 7.0), "MR_ConcreteDark")
    m.box("Architecture_Crown", (-0.5, 32.5, 0.8), (5.6, 1.2, 5.8), "MR_ConcreteDark")
    facade_ribs(m, "Facade", (-5.4, -3.5, -1.4, 1.4, 3.5, 5.4), 8.0, 15.5, -5.28)
    windows_front(m, "WindowsLower", (-4.5, -2.4, 0, 2.4, 4.5), (3, 6.2, 9.4, 12.6), -5.25, 0.55, 1.55, 9)
    windows_front(m, "WindowsUpper", (-3.6, -1.8, 0, 1.8, 3.6), (17, 20, 23), -3.82, 0.48, 1.45, 6)
    m.box("CyanLights_Spine", (0, 22.0, -4.1), (0.34, 19.0, 0.20), "MR_CyanEmission")
    m.box("Entrance", (0, 2.4, -5.3), (3.4, 4.8, 0.25), "MR_WindowDark")
    m.tube("Props_Antenna", (0.8, 33.1, 0.4), (1.0, 37.0, 0.4), 0.07, "MR_SecondaryMetal", 8)
    m.tube("Damage_Rebar", (-2.7, 33.0, -1.2), (-3.1, 35.0, -1.0), 0.07, "MR_Rust", 8)
    debris_cluster(m, "Debris", 4.5, -5.6, 12, 0.8)
    return m


def build_streetlamp() -> MeshBuilder:
    m = MeshBuilder("MemoryStreetLamp_A")
    m.tube("Architecture_Base", (0, 0, 0), (0, 0.35, 0), 0.38, "MR_ConcreteDark", 12)
    m.tube("Architecture_PoleLower", (0, 0.3, 0), (0, 4.7, 0), 0.12, "MR_DarkMetal", 12)
    m.tube("Architecture_PoleUpper", (0, 4.7, 0), (0.10, 5.8, 0), 0.085, "MR_SecondaryMetal", 10)
    m.tube("Architecture_Arm", (0.05, 5.45, 0), (1.25, 5.65, 0), 0.075, "MR_DarkMetal", 10)
    m.tube("Architecture_Brace", (0.12, 5.15, 0), (0.88, 5.58, 0), 0.045, "MR_SecondaryMetal", 8)
    m.box("Lamp_Housing", (1.30, 5.52, 0), (0.72, 0.32, 0.42), "MR_DarkMetal", rotation_y=-5)
    m.box("CyanLights_Lamp", (1.30, 5.34, 0), (0.52, 0.055, 0.28), "MR_CyanEmission", rotation_y=-5)
    m.box("CyanLights_PoleMarker", (0, 1.7, -0.13), (0.10, 0.65, 0.055), "MR_DimCyan")
    m.box("Damage_HangingPanel", (-0.18, 3.7, 0), (0.22, 0.55, 0.08), "MR_Rust", rotation_y=13)
    return m


def write_materials(path: Path) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    with path.open("w", encoding="utf-8", newline="\n") as handle:
        handle.write("# Memory Recycler 3D Archive City Kit materials\n")
        for name, values in MATERIALS.items():
            kd = values["kd"]
            ks = values["ks"]
            ke = values.get("ke", (0.0, 0.0, 0.0))
            handle.write(f"\nnewmtl {name}\n")
            handle.write(f"Ka {kd[0] * 0.15:.4f} {kd[1] * 0.15:.4f} {kd[2] * 0.15:.4f}\n")
            handle.write(f"Kd {kd[0]:.4f} {kd[1]:.4f} {kd[2]:.4f}\n")
            handle.write(f"Ks {ks[0]:.4f} {ks[1]:.4f} {ks[2]:.4f}\n")
            handle.write(f"Ke {ke[0]:.4f} {ke[1]:.4f} {ke[2]:.4f}\n")
            handle.write(f"Ns {values['ns']}\nillum 2\nd 1.0\n")


def _normalize(value: Vec3) -> Vec3:
    length = math.sqrt(sum(component * component for component in value))
    if length <= 1e-9:
        return (0.0, 0.0, 0.0)
    return tuple(component / length for component in value)  # type: ignore[return-value]


def _dot(a: Vec3, b: Vec3) -> float:
    return a[0] * b[0] + a[1] * b[1] + a[2] * b[2]


def _cross(a: Vec3, b: Vec3) -> Vec3:
    return (
        a[1] * b[2] - a[2] * b[1],
        a[2] * b[0] - a[0] * b[2],
        a[0] * b[1] - a[1] * b[0],
    )


def write_preview(mesh: MeshBuilder, path: Path, width: int = 1200, height: int = 800) -> None:
    """Write a simple orthographic material preview when Pillow is available."""
    try:
        from PIL import Image, ImageDraw  # type: ignore
    except ImportError as exc:
        raise RuntimeError("--preview requires Pillow. Use the bundled Codex Python runtime.") from exc

    minimum, maximum = mesh.bounds
    target = (
        (minimum[0] + maximum[0]) * 0.5,
        minimum[1] + (maximum[1] - minimum[1]) * 0.45,
        (minimum[2] + maximum[2]) * 0.5,
    )
    span = max(maximum[0] - minimum[0], maximum[1] - minimum[1], maximum[2] - minimum[2])
    camera = (target[0] + span * 1.15, target[1] + span * 0.72, target[2] - span * 1.55)
    forward = _normalize((target[0] - camera[0], target[1] - camera[1], target[2] - camera[2]))
    right = _normalize(_cross(forward, (0.0, 1.0, 0.0)))
    view_up = _normalize(_cross(right, forward))

    projected: list[tuple[float, float, float]] = []
    for vertex in mesh.vertices:
        relative = (vertex[0] - target[0], vertex[1] - target[1], vertex[2] - target[2])
        from_camera = (vertex[0] - camera[0], vertex[1] - camera[1], vertex[2] - camera[2])
        projected.append((_dot(relative, right), _dot(relative, view_up), _dot(from_camera, forward)))
    min_x = min(p[0] for p in projected)
    max_x = max(p[0] for p in projected)
    min_y = min(p[1] for p in projected)
    max_y = max(p[1] for p in projected)
    margin = 54
    scale = min((width - margin * 2) / max(1e-6, max_x - min_x), (height - margin * 2) / max(1e-6, max_y - min_y))
    offset_x = width * 0.5 - (min_x + max_x) * 0.5 * scale
    offset_y = height * 0.5 + (min_y + max_y) * 0.5 * scale

    image = Image.new("RGB", (width, height), (11, 16, 20))
    draw = ImageDraw.Draw(image)
    draw.rectangle((0, int(height * 0.79), width, height), fill=(15, 20, 23))
    light = _normalize((-0.4, 0.85, -0.5))
    visible_faces = []
    for face in mesh.faces:
        if len(face.indices) < 3:
            continue
        world = [mesh.vertices[index - 1] for index in face.indices]
        edge_a = tuple(world[1][i] - world[0][i] for i in range(3))
        edge_b = tuple(world[2][i] - world[0][i] for i in range(3))
        normal = _normalize(_cross(edge_a, edge_b))  # type: ignore[arg-type]
        center = tuple(sum(vertex[i] for vertex in world) / len(world) for i in range(3))
        to_camera = _normalize(tuple(camera[i] - center[i] for i in range(3)))  # type: ignore[arg-type]
        if _dot(normal, to_camera) <= 0.002:
            continue
        depth = sum(projected[index - 1][2] for index in face.indices) / len(face.indices)
        visible_faces.append((depth, face, normal))

    for _depth, face, normal in sorted(visible_faces, key=lambda item: item[0], reverse=True):
        points = [
            (projected[index - 1][0] * scale + offset_x, offset_y - projected[index - 1][1] * scale)
            for index in face.indices
        ]
        base = MATERIALS[face.material]["kd"]
        brightness = 0.34 + 0.66 * max(0.0, _dot(normal, light))
        if "ke" in MATERIALS[face.material]:
            brightness = max(brightness, 0.88)
        color = tuple(max(0, min(255, int(channel * brightness * 255))) for channel in base)
        draw.polygon(points, fill=color, outline=(20, 27, 31))

    draw.text((24, 20), f"{mesh.name}  |  {mesh.triangle_count:,} tris  |  1 unit = 1m", fill=(190, 222, 224))
    path.parent.mkdir(parents=True, exist_ok=True)
    image.save(path)


def blender_export(models: Sequence[MeshBuilder], output_dir: Path, project_root: Path) -> None:
    try:
        import bpy  # type: ignore
    except ImportError as exc:
        raise RuntimeError("--blender-export requires Blender's Python runtime (bpy).") from exc

    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    for collection in list(bpy.data.collections):
        if collection.name != "Collection":
            bpy.data.collections.remove(collection)

    material_cache = {}
    for material_name, values in MATERIALS.items():
        material = bpy.data.materials.new(material_name)
        material.diffuse_color = (*values["kd"], 1.0)
        material.use_nodes = True
        principled = material.node_tree.nodes.get("Principled BSDF")
        if principled:
            principled.inputs["Base Color"].default_value = (*values["kd"], 1.0)
            principled.inputs["Roughness"].default_value = 0.72
            if "ke" in values:
                emission_input = principled.inputs.get("Emission Color") or principled.inputs.get("Emission")
                if emission_input:
                    emission_input.default_value = (*values["ke"], 1.0)
                strength_input = principled.inputs.get("Emission Strength")
                if strength_input:
                    strength_input.default_value = 5.0
        material_cache[material_name] = material

    for model in models:
        root_collection = bpy.data.collections.new(model.name)
        bpy.context.scene.collection.children.link(root_collection)
        groups: dict[str, list[Face]] = {}
        for face in model.faces:
            groups.setdefault(category_name(face.group), []).append(face)
        for group_name, group_faces in groups.items():
            used_global_indices = sorted({index for face in group_faces for index in face.indices})
            index_map = {global_index: local_index for local_index, global_index in enumerate(used_global_indices)}
            vertices = [model.vertices[index - 1] for index in used_global_indices]
            polygons = [tuple(index_map[index] for index in face.indices) for face in group_faces]
            mesh_data = bpy.data.meshes.new(group_name)
            mesh_data.from_pydata(vertices, [], polygons)
            mesh_data.update()
            obj = bpy.data.objects.new(group_name, mesh_data)
            root_collection.objects.link(obj)
            material_names = []
            for face in group_faces:
                if face.material not in material_names:
                    material_names.append(face.material)
            for material_name in material_names:
                mesh_data.materials.append(material_cache[material_name])
            for poly, face in zip(mesh_data.polygons, group_faces):
                poly.material_index = material_names.index(face.material)

    source_dir = project_root / "SourceAssets" / "Blender"
    source_dir.mkdir(parents=True, exist_ok=True)
    blend_path = source_dir / "ArchiveCityKit.blend"
    bpy.ops.wm.save_as_mainfile(filepath=str(blend_path))

    for model in models:
        bpy.ops.object.select_all(action="DESELECT")
        collection = bpy.data.collections.get(model.name)
        if not collection:
            continue
        for obj in collection.objects:
            obj.select_set(True)
        bpy.ops.export_scene.fbx(
            filepath=str(output_dir / f"{model.name}.fbx"),
            use_selection=True,
            apply_unit_scale=True,
            bake_space_transform=False,
            add_leaf_bones=False,
            axis_forward="-Z",
            axis_up="Y",
        )


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--output", type=Path, default=None)
    parser.add_argument("--blender-export", action="store_true")
    parser.add_argument("--preview", action="store_true")
    cli_args = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else sys.argv[1:]
    args, _unknown = parser.parse_known_args(cli_args)

    project_root = Path(__file__).resolve().parents[2]
    output_dir = args.output or project_root / "Assets" / "MemoryRecycler3D" / "Models" / "ArchiveCityKit" / "Generated"
    output_dir.mkdir(parents=True, exist_ok=True)
    material_path = output_dir / "MR3D_ArchiveCityKit.mtl"
    write_materials(material_path)

    models = [build_archive(), build_lowrise(), build_midrise(), build_tower(), build_streetlamp()]
    for model in models:
        model.write_obj(output_dir / f"{model.name}.obj", material_path.name)
        if args.preview:
            write_preview(model, output_dir.parent / "Previews" / f"{model.name}_Preview.png")
        minimum, maximum = model.bounds
        size = tuple(maximum[i] - minimum[i] for i in range(3))
        print(
            f"{model.name}: vertices={len(model.vertices)} faces={len(model.faces)} "
            f"triangles={model.triangle_count} size={size[0]:.2f}x{size[2]:.2f}x{size[1]:.2f}m"
        )

    if args.blender_export:
        blender_export(models, output_dir, project_root)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
