using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Builds a deterministic ruined city with collision-safe spacing around the archive.
/// Everything is generated at runtime so Prototype3D.unity stays untouched.
/// </summary>
public static class ArchiveCityBackdropExpansion3D
{
    public const bool ExpandedCityEnabled = true;

    private static readonly Vector3 ArchiveCenter = new Vector3(0f, 0f, 35.5f);
    private static readonly Vector3 EmergencyRespawn = new Vector3(0f, 0.15f, -22f);
    private const float ExpandedBoundaryRadius = 225f;
    private const float ExpandedGroundSize = 480f;
    private const float BuildingClearance = 6f;
    private const float AvenueHalfWidth = 19f;
    private const float MemoryOrbBuildingClearance = 2.2f;
    private const float MemoryOrbMinimumSpacing = 4.2f;
    private const string BackdropRootName = "Expanded Ruined City Backdrop";
    private const string ArchiveTemplateName = "Trial Central Memory Archive";
    private const string LowriseTemplateName = "Trial Ruined Lowrise_L";
    private const string MidriseTemplateName = "Trial Ruined Midrise_R";
    private const string TowerTemplateName = "Trial Ruined Tower_L";
    private const string LampTemplateName = "Trial Tripo Street Lamp_L0";

    private struct RingSpec
    {
        public readonly float radius;
        public readonly int count;
        public readonly float angleOffset;
        public readonly float minScale;
        public readonly float maxScale;
        public readonly float avenueGap;

        public RingSpec(float radius, int count, float angleOffset, float minScale, float maxScale, float avenueGap)
        {
            this.radius = radius;
            this.count = count;
            this.angleOffset = angleOffset;
            this.minScale = minScale;
            this.maxScale = maxScale;
            this.avenueGap = avenueGap;
        }
    }

    private struct Footprint
    {
        public readonly Vector2 center;
        public readonly float radius;

        public Footprint(Vector3 position, float radius)
        {
            center = new Vector2(position.x, position.z);
            this.radius = radius;
        }
    }

    private static readonly RingSpec[] DistrictRings =
    {
        new RingSpec(86f, 12, 8f, 0.92f, 1.08f, 22f),
        new RingSpec(132f, 18, 3f, 1.00f, 1.22f, 16f),
        new RingSpec(180f, 24, 6f, 1.08f, 1.38f, 10f)
    };

    public static void Build(Transform trialRoot)
    {
        if (!ExpandedCityEnabled || trialRoot == null)
            return;

        Transform archive = FindDescendant(trialRoot, ArchiveTemplateName);
        Transform lowrise = FindDescendant(trialRoot, LowriseTemplateName);
        Transform midrise = FindDescendant(trialRoot, MidriseTemplateName);
        Transform tower = FindDescendant(trialRoot, TowerTemplateName);
        Transform lamp = FindDescendant(trialRoot, LampTemplateName);

        if (archive == null || lowrise == null || midrise == null || tower == null || lamp == null)
        {
            Debug.LogWarning("Expanded archive city skipped because one or more trial model templates are missing.");
            return;
        }

        GameObject backdrop = new GameObject(BackdropRootName);
        backdrop.transform.SetParent(trialRoot, false);

        List<Collider> buildingBlockers = new List<Collider>();
        int coreColliderCount = AddCoreTrialColliders(trialRoot, buildingBlockers);
        int relocatedCoreBuildingCount = ResolveCoreBuildingOverlaps(buildingBlockers);
        Material roadMaterial = ResolveRoadMaterial(lowrise);
        CreateExpandedGround(backdrop.transform, roadMaterial);
        ConfigureExpandedBoundary();

        List<Footprint> occupied = CollectExistingBuildingFootprints(backdrop.transform);
        int rejectedPlacements;
        int buildingCount = CreateDistrictBuildings(
            backdrop.transform,
            lowrise,
            midrise,
            tower,
            occupied,
            buildingBlockers,
            out rejectedPlacements);
        int lampCount = CreateDistrictLamps(backdrop.transform, lamp);
        int roadCount = CreateRingRoads(backdrop.transform, roadMaterial);

        Physics.SyncTransforms();
        int colliderOverlapCount = CountColliderOverlaps(buildingBlockers);
        int relocatedMemoryOrbCount = RepositionMemoryOrbsOutsideBuildings(buildingBlockers);
        RescuePlayerIfInsideBuilding(buildingBlockers);

        if (colliderOverlapCount > 0)
            Debug.LogError($"Archive city collider spacing audit failed: overlaps={colliderOverlapCount}.");

        Debug.Log(
            $"Expanded archive city loaded: buildings={buildingCount}, lamps={lampCount}, " +
            $"ringRoads={roadCount}, buildingColliders={buildingBlockers.Count}, " +
            $"coreColliders={coreColliderCount}, rejectedOverlaps={rejectedPlacements}, " +
            $"relocatedCoreBuildings={relocatedCoreBuildingCount}, " +
            $"colliderOverlaps={colliderOverlapCount}, " +
            $"relocatedMemoryOrbs={relocatedMemoryOrbCount}, " +
            $"boundaryRadius={ExpandedBoundaryRadius:0}m.");
    }

    private static int AddCoreTrialColliders(Transform trialRoot, List<Collider> blockers)
    {
        int count = 0;
        string[] buildingNames =
        {
            ArchiveTemplateName,
            LowriseTemplateName,
            MidriseTemplateName,
            TowerTemplateName
        };

        foreach (string buildingName in buildingNames)
        {
            Transform building = FindDescendant(trialRoot, buildingName);
            BoxCollider collider = AddBuildingCollider(building);
            if (collider == null)
                continue;

            blockers.Add(collider);
            count++;
        }

        Transform[] transforms = trialRoot.GetComponentsInChildren<Transform>(true);
        foreach (Transform candidate in transforms)
        {
            if (candidate.name.StartsWith("Trial Tripo Street Lamp_"))
                AddLampCollider(candidate);
        }

        return count;
    }

    private static int ResolveCoreBuildingOverlaps(List<Collider> coreBlockers)
    {
        Physics.SyncTransforms();
        int relocated = 0;

        // Index zero is the landmark archive and remains fixed. Smaller supporting
        // buildings are nudged away from it before the outer districts are placed.
        for (int i = 1; i < coreBlockers.Count; i++)
        {
            Collider candidate = coreBlockers[i];
            if (candidate == null || !HasOverlapWithOtherCollider(candidate, coreBlockers))
                continue;

            Vector3 originalPosition = candidate.transform.position;
            Vector3 outward = candidate.bounds.center - ArchiveCenter;
            outward.y = 0f;
            if (outward.sqrMagnitude < 0.01f)
                outward = Vector3.left;
            else
                outward.Normalize();

            bool foundClearPosition = false;
            for (int attempt = 1; attempt <= 12; attempt++)
            {
                candidate.transform.position = originalPosition + outward * (attempt * 4f);
                Physics.SyncTransforms();
                if (HasOverlapWithOtherCollider(candidate, coreBlockers))
                    continue;

                foundClearPosition = true;
                relocated++;
                Debug.Log(
                    $"Moved overlapping core building '{candidate.transform.name}': " +
                    $"{originalPosition} -> {candidate.transform.position}.");
                break;
            }

            if (foundClearPosition)
                continue;

            candidate.transform.position = originalPosition;
            Physics.SyncTransforms();
            Debug.LogError($"Could not resolve core building overlap for '{candidate.transform.name}'.");
        }

        return relocated;
    }

    private static bool HasOverlapWithOtherCollider(Collider candidate, List<Collider> colliders)
    {
        foreach (Collider other in colliders)
        {
            if (other == null || other == candidate || !other.enabled)
                continue;

            bool overlaps = Physics.ComputePenetration(
                candidate,
                candidate.transform.position,
                candidate.transform.rotation,
                other,
                other.transform.position,
                other.transform.rotation,
                out _,
                out float distance);
            if (overlaps && distance > 0.02f)
                return true;
        }

        return false;
    }

    private static int CreateDistrictBuildings(
        Transform parent,
        Transform lowrise,
        Transform midrise,
        Transform tower,
        List<Footprint> occupied,
        List<Collider> blockers,
        out int rejectedPlacements)
    {
        Transform[] templates = { lowrise, midrise, tower };
        float[] templateRadii =
        {
            GetLocalFootprintRadius(lowrise),
            GetLocalFootprintRadius(midrise),
            GetLocalFootprintRadius(tower)
        };
        int created = 0;
        rejectedPlacements = 0;

        for (int ringIndex = 0; ringIndex < DistrictRings.Length; ringIndex++)
        {
            RingSpec ring = DistrictRings[ringIndex];
            GameObject ringRoot = new GameObject($"Ruined District Ring {ringIndex + 1}");
            ringRoot.transform.SetParent(parent, false);

            for (int i = 0; i < ring.count; i++)
            {
                float baseAngle = ring.angleOffset + i * (360f / ring.count);
                if (Mathf.Abs(Mathf.DeltaAngle(baseAngle, 180f)) < ring.avenueGap)
                    continue;

                int templateIndex = SelectTemplateIndex(ringIndex, i);
                float scale = Mathf.Lerp(ring.minScale, ring.maxScale, Hash01(ringIndex, i, 3));
                if (ringIndex == DistrictRings.Length - 1 && templateIndex == 2)
                    scale *= 1.10f;

                float footprintRadius = templateRadii[templateIndex] * scale;
                if (!TryFindClearPosition(ringIndex, i, ring, footprintRadius, occupied, out Vector3 position, out float angle))
                {
                    rejectedPlacements++;
                    continue;
                }

                Transform instance = Object.Instantiate(templates[templateIndex].gameObject, ringRoot.transform).transform;
                instance.name = $"Backdrop Building R{ringIndex + 1}_{i:00}";
                instance.position = position;
                instance.rotation = Quaternion.Euler(0f, angle + 90f, 0f);
                instance.localScale = Vector3.one * scale;

                PrepareDistantVisual(instance.gameObject, ringIndex > 0);
                BoxCollider collider = AddBuildingCollider(instance);
                if (collider != null)
                    blockers.Add(collider);

                occupied.Add(new Footprint(position, footprintRadius));
                created++;
            }
        }

        return created;
    }

    private static bool TryFindClearPosition(
        int ringIndex,
        int itemIndex,
        RingSpec ring,
        float footprintRadius,
        List<Footprint> occupied,
        out Vector3 position,
        out float angle)
    {
        float baseAngle = ring.angleOffset + itemIndex * (360f / ring.count);
        float radialJitter = Mathf.Lerp(-2.5f, 2.5f, Hash01(ringIndex, itemIndex, 1));

        for (int attempt = 0; attempt < 12; attempt++)
        {
            int direction = attempt % 2 == 0 ? -1 : 1;
            float angularOffset = attempt == 0 ? 0f : direction * ((attempt + 1) / 2) * 2.8f;
            float outwardOffset = (attempt / 4) * 8f;
            angle = baseAngle + angularOffset;
            Vector3 directionVector = DirectionFromAngle(angle);
            position = ArchiveCenter + directionVector * (ring.radius + radialJitter + outwardOffset);

            if (IsAvenueClearZone(position, footprintRadius) || OverlapsAny(position, footprintRadius, occupied))
                continue;

            return true;
        }

        position = Vector3.zero;
        angle = baseAngle;
        return false;
    }

    private static bool IsAvenueClearZone(Vector3 position, float footprintRadius)
    {
        bool withinAvenueLength = position.z > -48f && position.z < ArchiveCenter.z + 27f;
        return withinAvenueLength && Mathf.Abs(position.x) < AvenueHalfWidth + footprintRadius;
    }

    private static bool OverlapsAny(Vector3 position, float footprintRadius, List<Footprint> occupied)
    {
        Vector2 center = new Vector2(position.x, position.z);
        foreach (Footprint other in occupied)
        {
            float requiredDistance = footprintRadius + other.radius + BuildingClearance;
            if ((center - other.center).sqrMagnitude < requiredDistance * requiredDistance)
                return true;
        }

        return false;
    }

    private static List<Footprint> CollectExistingBuildingFootprints(Transform excludedRoot)
    {
        List<Footprint> occupied = new List<Footprint>();
        Transform[] sceneTransforms = Object.FindObjectsByType<Transform>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (Transform candidate in sceneTransforms)
        {
            if (candidate == null || candidate.IsChildOf(excludedRoot) || !IsBuildingRoot(candidate.name))
                continue;

            if (!TryGetWorldVisualBounds(candidate, out Bounds bounds))
                continue;

            float radius = Mathf.Sqrt(bounds.extents.x * bounds.extents.x + bounds.extents.z * bounds.extents.z);
            occupied.Add(new Footprint(bounds.center, radius));
        }

        return occupied;
    }

    private static bool IsBuildingRoot(string objectName)
    {
        return objectName.StartsWith("Silent Building_") ||
               objectName.StartsWith("Cinematic Ring District Building") ||
               objectName.StartsWith("Cinematic Background Block") ||
               objectName.StartsWith("Tripo Background Block") ||
               objectName.StartsWith("Trial Ruined") ||
               objectName == ArchiveTemplateName;
    }

    private static int CreateDistrictLamps(Transform parent, Transform lampTemplate)
    {
        GameObject lampRoot = new GameObject("Expanded District Street Lamps");
        lampRoot.transform.SetParent(parent, false);
        int created = 0;

        created += CreateLampRing(lampRoot.transform, lampTemplate, 70f, 14, 11f, 1.00f);
        created += CreateLampRing(lampRoot.transform, lampTemplate, 114f, 18, 4f, 1.10f);
        return created;
    }

    private static int CreateLampRing(Transform parent, Transform template, float radius, int count, float offset, float scale)
    {
        int created = 0;
        for (int i = 0; i < count; i++)
        {
            float angle = offset + i * (360f / count);
            if (Mathf.Abs(Mathf.DeltaAngle(angle, 180f)) < 13f)
                continue;

            Vector3 direction = DirectionFromAngle(angle);
            Transform lamp = Object.Instantiate(template.gameObject, parent).transform;
            lamp.name = $"Backdrop Street Lamp {radius:000}_{i:00}";
            lamp.position = ArchiveCenter + direction * radius;
            lamp.rotation = Quaternion.Euler(0f, angle + 90f, 0f);
            lamp.localScale = Vector3.one * scale;
            PrepareDistantVisual(lamp.gameObject, radius > 90f);
            AddLampCollider(lamp);
            created++;
        }

        return created;
    }

    private static void CreateExpandedGround(Transform parent, Material material)
    {
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ground.name = "Expanded City Ground";
        ground.transform.SetParent(parent, false);
        ground.transform.position = ArchiveCenter + new Vector3(0f, -0.35f, 0f);
        ground.transform.localScale = new Vector3(ExpandedGroundSize, 0.60f, ExpandedGroundSize);

        Renderer renderer = ground.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = true;
        }
    }

    private static void ConfigureExpandedBoundary()
    {
        PlayBoundary3D boundary = PlayBoundary3D.Instance;
        if (boundary == null)
            return;

        boundary.Configure(ArchiveCenter, ExpandedBoundaryRadius, boundary.safeRespawnPosition, boundary.safeRespawnYaw);
        boundary.floorY = -12f;
    }

    private static int CreateRingRoads(Transform parent, Material roadMaterial)
    {
        if (roadMaterial == null)
            return 0;

        GameObject roadsRoot = new GameObject("Expanded Concentric Roads");
        roadsRoot.transform.SetParent(parent, false);

        CreateRingRoad(roadsRoot.transform, "Inner Archive Ring Road", 70f, 6.2f, roadMaterial);
        CreateRingRoad(roadsRoot.transform, "Middle District Ring Road", 114f, 7.0f, roadMaterial);
        CreateRingRoad(roadsRoot.transform, "Outer District Ring Road", 160f, 8.0f, roadMaterial);
        return 3;
    }

    private static void CreateRingRoad(Transform parent, string name, float radius, float width, Material material)
    {
        const int segments = 128;
        Vector3[] vertices = new Vector3[(segments + 1) * 2];
        Vector2[] uv = new Vector2[vertices.Length];
        int[] triangles = new int[segments * 6];
        float inner = radius - width * 0.5f;
        float outer = radius + width * 0.5f;

        for (int i = 0; i <= segments; i++)
        {
            float t = i / (float)segments;
            float ringAngle = t * Mathf.PI * 2f;
            float x = Mathf.Sin(ringAngle);
            float z = Mathf.Cos(ringAngle);
            vertices[i * 2] = ArchiveCenter + new Vector3(x * inner, 0.035f, z * inner);
            vertices[i * 2 + 1] = ArchiveCenter + new Vector3(x * outer, 0.035f, z * outer);
            uv[i * 2] = new Vector2(0f, t * 16f);
            uv[i * 2 + 1] = new Vector2(1f, t * 16f);

            if (i == segments)
                continue;

            int vertex = i * 2;
            int triangle = i * 6;
            triangles[triangle] = vertex;
            triangles[triangle + 1] = vertex + 2;
            triangles[triangle + 2] = vertex + 1;
            triangles[triangle + 3] = vertex + 1;
            triangles[triangle + 4] = vertex + 2;
            triangles[triangle + 5] = vertex + 3;
        }

        Mesh mesh = new Mesh { name = name + " Mesh" };
        mesh.vertices = vertices;
        mesh.uv = uv;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        GameObject road = new GameObject(name);
        road.transform.SetParent(parent, false);
        road.AddComponent<MeshFilter>().sharedMesh = mesh;
        MeshRenderer renderer = road.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = true;
    }

    private static BoxCollider AddBuildingCollider(Transform building)
    {
        if (building == null || !TryGetLocalVisualBounds(building, out Bounds bounds))
            return null;

        BoxCollider rootCollider = building.GetComponent<BoxCollider>();
        if (rootCollider == null)
            rootCollider = building.gameObject.AddComponent<BoxCollider>();

        foreach (Collider collider in building.GetComponentsInChildren<Collider>(true))
        {
            if (collider != rootCollider)
                collider.enabled = false;
        }

        rootCollider.isTrigger = false;
        rootCollider.center = bounds.center;
        rootCollider.size = new Vector3(
            Mathf.Max(0.5f, bounds.size.x),
            Mathf.Max(1f, bounds.size.y),
            Mathf.Max(0.5f, bounds.size.z));
        rootCollider.enabled = true;
        return rootCollider;
    }

    private static CapsuleCollider AddLampCollider(Transform lamp)
    {
        if (lamp == null || !TryGetLocalVisualBounds(lamp, out Bounds bounds))
            return null;

        CapsuleCollider rootCollider = lamp.GetComponent<CapsuleCollider>();
        if (rootCollider == null)
            rootCollider = lamp.gameObject.AddComponent<CapsuleCollider>();

        foreach (Collider collider in lamp.GetComponentsInChildren<Collider>(true))
        {
            if (collider != rootCollider)
                collider.enabled = false;
        }

        rootCollider.isTrigger = false;
        rootCollider.direction = 1;
        rootCollider.center = new Vector3(bounds.center.x, bounds.center.y, bounds.center.z);
        rootCollider.radius = Mathf.Clamp(Mathf.Min(bounds.size.x, bounds.size.z) * 0.32f, 0.20f, 0.36f);
        rootCollider.height = Mathf.Max(rootCollider.radius * 2f, bounds.size.y * 0.96f);
        rootCollider.enabled = true;
        return rootCollider;
    }

    private static void RescuePlayerIfInsideBuilding(List<Collider> blockers)
    {
        GameObject player = GameObject.Find("Player_Recycler");
        if (player == null)
            return;

        CharacterController controller = player.GetComponent<CharacterController>();
        Vector3 samplePoint = controller != null ? controller.bounds.center : player.transform.position + Vector3.up;
        bool inside = false;
        foreach (Collider blocker in blockers)
        {
            if (blocker == null || !blocker.enabled)
                continue;

            Vector3 closest = blocker.ClosestPoint(samplePoint);
            if ((closest - samplePoint).sqrMagnitude <= 0.000001f)
            {
                inside = true;
                break;
            }
        }

        if (!inside)
            return;

        Vector3 target = PlayBoundary3D.Instance != null
            ? PlayBoundary3D.Instance.safeRespawnPosition
            : EmergencyRespawn;
        if (controller != null)
            controller.enabled = false;
        player.transform.position = target;
        if (controller != null)
            controller.enabled = true;
        Debug.LogWarning("Player started inside an archive city collider and was moved to the safe respawn point.");
    }

    private static int RepositionMemoryOrbsOutsideBuildings(List<Collider> blockers)
    {
        List<MemoryObject3D> memoryOrbs = new List<MemoryObject3D>(
            Object.FindObjectsByType<MemoryObject3D>(FindObjectsInactive.Include, FindObjectsSortMode.None));
        memoryOrbs.Sort((left, right) => string.CompareOrdinal(left.name, right.name));

        Bounds archiveBounds = new Bounds(
            ArchiveCenter + Vector3.up * 16f,
            new Vector3(50f, 32f, 42f));
        foreach (Collider blocker in blockers)
        {
            if (blocker != null && blocker.transform.name == ArchiveTemplateName)
            {
                archiveBounds = blocker.bounds;
                break;
            }
        }

        List<Vector3> reservedPositions = new List<Vector3>();
        List<MemoryObject3D> blockedOrbs = new List<MemoryObject3D>();
        foreach (MemoryObject3D memoryOrb in memoryOrbs)
        {
            if (memoryOrb == null)
                continue;

            float radius = GetMemoryOrbRadius(memoryOrb);
            if (IsClearOfBuildings(memoryOrb.transform.position, radius, blockers))
                reservedPositions.Add(memoryOrb.transform.position);
            else
                blockedOrbs.Add(memoryOrb);
        }

        int relocated = 0;
        for (int i = 0; i < blockedOrbs.Count; i++)
        {
            MemoryObject3D memoryOrb = blockedOrbs[i];
            float radius = GetMemoryOrbRadius(memoryOrb);
            if (!TryFindSafeMemoryOrbPosition(
                    memoryOrb.transform.position,
                    radius,
                    archiveBounds,
                    blockers,
                    reservedPositions,
                    i,
                    out Vector3 safePosition))
            {
                Debug.LogError($"Could not find a collision-free position for {memoryOrb.name}.");
                continue;
            }

            Vector3 previousPosition = memoryOrb.transform.position;
            memoryOrb.RelocateForRuntimeLayout(safePosition);
            reservedPositions.Add(safePosition);
            relocated++;
            Debug.Log(
                $"Moved {memoryOrb.name} outside the archive collider: " +
                $"{previousPosition} -> {safePosition}.");
        }

        return relocated;
    }

    private static bool TryFindSafeMemoryOrbPosition(
        Vector3 originalPosition,
        float orbRadius,
        Bounds archiveBounds,
        List<Collider> blockers,
        List<Vector3> reservedPositions,
        int orbIndex,
        out Vector3 safePosition)
    {
        List<Vector3> candidates = BuildMemoryOrbCandidates(archiveBounds, originalPosition.y, orbIndex);
        float bestDistance = float.PositiveInfinity;
        safePosition = originalPosition;
        bool found = false;

        foreach (Vector3 candidate in candidates)
        {
            if (!IsClearOfBuildings(candidate, orbRadius, blockers) ||
                !IsClearOfOtherMemoryOrbs(candidate, reservedPositions))
            {
                continue;
            }

            float distance = (candidate - originalPosition).sqrMagnitude;
            if (distance >= bestDistance)
                continue;

            bestDistance = distance;
            safePosition = candidate;
            found = true;
        }

        return found;
    }

    private static List<Vector3> BuildMemoryOrbCandidates(Bounds archiveBounds, float height, int orbIndex)
    {
        List<Vector3> candidates = new List<Vector3>();
        float outsideOffset = MemoryOrbBuildingClearance + 3.2f;
        float[] facadeOffsets = { -0.72f, -0.36f, 0.36f, 0.72f };
        foreach (float normalizedOffset in facadeOffsets)
        {
            float x = archiveBounds.center.x + archiveBounds.extents.x * normalizedOffset;
            candidates.Add(new Vector3(x, height, archiveBounds.min.z - outsideOffset));
            candidates.Add(new Vector3(x, height, archiveBounds.max.z + outsideOffset));
        }

        float[] sideOffsets = { -0.65f, 0f, 0.65f };
        foreach (float normalizedOffset in sideOffsets)
        {
            float z = archiveBounds.center.z + archiveBounds.extents.z * normalizedOffset;
            candidates.Add(new Vector3(archiveBounds.min.x - outsideOffset, height, z));
            candidates.Add(new Vector3(archiveBounds.max.x + outsideOffset, height, z));
        }

        float baseRadius = Mathf.Max(archiveBounds.extents.x, archiveBounds.extents.z) + outsideOffset + 3f;
        const int ringSegments = 24;
        for (int ring = 0; ring < 5; ring++)
        {
            float radius = baseRadius + ring * 10f;
            float angleOffset = orbIndex * (360f / ringSegments / 2f);
            for (int segment = 0; segment < ringSegments; segment++)
            {
                float angle = angleOffset + segment * (360f / ringSegments);
                candidates.Add(archiveBounds.center + DirectionFromAngle(angle) * radius +
                               Vector3.up * (height - archiveBounds.center.y));
            }
        }

        return candidates;
    }

    private static bool IsClearOfBuildings(Vector3 position, float orbRadius, List<Collider> blockers)
    {
        float requiredClearance = orbRadius + MemoryOrbBuildingClearance;
        float requiredClearanceSquared = requiredClearance * requiredClearance;
        foreach (Collider blocker in blockers)
        {
            if (blocker == null || !blocker.enabled)
                continue;

            Vector3 closest = blocker.ClosestPoint(position);
            if ((closest - position).sqrMagnitude < requiredClearanceSquared)
                return false;
        }

        return true;
    }

    private static bool IsClearOfOtherMemoryOrbs(Vector3 position, List<Vector3> reservedPositions)
    {
        float requiredSpacingSquared = MemoryOrbMinimumSpacing * MemoryOrbMinimumSpacing;
        Vector2 candidate = new Vector2(position.x, position.z);
        foreach (Vector3 reservedPosition in reservedPositions)
        {
            Vector2 reserved = new Vector2(reservedPosition.x, reservedPosition.z);
            if ((candidate - reserved).sqrMagnitude < requiredSpacingSquared)
                return false;
        }

        return true;
    }

    private static float GetMemoryOrbRadius(MemoryObject3D memoryOrb)
    {
        Collider collider = memoryOrb.GetComponent<Collider>();
        if (collider == null || !collider.enabled || !memoryOrb.gameObject.activeInHierarchy)
            return 0.9f;

        return Mathf.Max(0.9f, collider.bounds.extents.x, collider.bounds.extents.z);
    }

    private static int CountColliderOverlaps(List<Collider> blockers)
    {
        int overlapCount = 0;
        for (int i = 0; i < blockers.Count; i++)
        {
            Collider first = blockers[i];
            if (first == null || !first.enabled)
                continue;

            for (int j = i + 1; j < blockers.Count; j++)
            {
                Collider second = blockers[j];
                if (second == null || !second.enabled)
                    continue;

                bool overlaps = Physics.ComputePenetration(
                    first,
                    first.transform.position,
                    first.transform.rotation,
                    second,
                    second.transform.position,
                    second.transform.rotation,
                    out _,
                    out float distance);
                if (overlaps && distance > 0.02f)
                {
                    Debug.LogError(
                        $"Archive city collider overlap: '{first.transform.name}' with " +
                        $"'{second.transform.name}', penetration={distance:0.00}m.");
                    overlapCount++;
                }
            }
        }

        return overlapCount;
    }

    private static void PrepareDistantVisual(GameObject instance, bool disableShadows)
    {
        foreach (Light light in instance.GetComponentsInChildren<Light>(true))
            light.enabled = false;

        if (!disableShadows)
            return;

        foreach (Renderer renderer in instance.GetComponentsInChildren<Renderer>(true))
            renderer.shadowCastingMode = ShadowCastingMode.Off;
    }

    private static Material ResolveRoadMaterial(Transform fallback)
    {
        GameObject avenue = GameObject.Find("Main Avenue");
        Renderer avenueRenderer = avenue != null ? avenue.GetComponent<Renderer>() : null;
        if (avenueRenderer != null && avenueRenderer.sharedMaterial != null)
            return avenueRenderer.sharedMaterial;

        Renderer fallbackRenderer = fallback.GetComponentInChildren<Renderer>(true);
        return fallbackRenderer != null ? fallbackRenderer.sharedMaterial : null;
    }

    private static float GetLocalFootprintRadius(Transform root)
    {
        if (!TryGetLocalVisualBounds(root, out Bounds bounds))
            return 8f;

        return Mathf.Sqrt(bounds.extents.x * bounds.extents.x + bounds.extents.z * bounds.extents.z);
    }

    private static bool TryGetLocalVisualBounds(Transform root, out Bounds localBounds)
    {
        localBounds = new Bounds(Vector3.zero, Vector3.zero);
        bool hasBounds = false;
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer renderer in renderers)
        {
            if (renderer is ParticleSystemRenderer)
                continue;

            Bounds world = renderer.bounds;
            Vector3 min = world.min;
            Vector3 max = world.max;
            for (int x = 0; x < 2; x++)
            for (int y = 0; y < 2; y++)
            for (int z = 0; z < 2; z++)
            {
                Vector3 corner = new Vector3(x == 0 ? min.x : max.x, y == 0 ? min.y : max.y, z == 0 ? min.z : max.z);
                Vector3 local = root.InverseTransformPoint(corner);
                if (!hasBounds)
                {
                    localBounds = new Bounds(local, Vector3.zero);
                    hasBounds = true;
                }
                else
                {
                    localBounds.Encapsulate(local);
                }
            }
        }

        return hasBounds;
    }

    private static bool TryGetWorldVisualBounds(Transform root, out Bounds bounds)
    {
        bounds = new Bounds(root.position, Vector3.zero);
        bool hasBounds = false;
        foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
        {
            if (!renderer.enabled || renderer is ParticleSystemRenderer)
                continue;

            if (!hasBounds)
            {
                bounds = renderer.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        return hasBounds;
    }

    private static int SelectTemplateIndex(int ringIndex, int itemIndex)
    {
        if (ringIndex == DistrictRings.Length - 1)
            return itemIndex % 3 == 0 ? 2 : itemIndex % 2;

        return (itemIndex + ringIndex) % 5 == 0 ? 2 : (itemIndex + ringIndex) % 2;
    }

    private static Transform FindDescendant(Transform root, string objectName)
    {
        Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
        foreach (Transform candidate in transforms)
        {
            if (candidate.name == objectName)
                return candidate;
        }

        return null;
    }

    private static Vector3 DirectionFromAngle(float angle)
    {
        float radians = angle * Mathf.Deg2Rad;
        return new Vector3(Mathf.Sin(radians), 0f, Mathf.Cos(radians));
    }

    private static float Hash01(int ring, int index, int salt)
    {
        float value = Mathf.Sin((ring + 1) * 17.17f + (index + 3) * 41.73f + salt * 13.91f) * 43758.5453f;
        return value - Mathf.Floor(value);
    }
}
