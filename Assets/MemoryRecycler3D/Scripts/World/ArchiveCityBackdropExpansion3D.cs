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
    private const float ExpandedBoundaryRadius = 610f;
    private const float ExpandedGroundSize = 1320f;
    private const float OuterCityRadius = 560f;
    private const float BuildingClearance = 7.5f;
    private const float InnerCivicClearRadius = 32f;
    private const float PrimaryAvenueHalfWidth = 11f;
    private const float SecondaryAvenueHalfWidth = 7.5f;
    private const int RadialAvenueCount = 8;
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

    private readonly struct OpenSpaceSpec
    {
        public readonly string name;
        public readonly float radius;
        public readonly float angle;
        public readonly Vector2 size;
        public readonly bool playground;

        public OpenSpaceSpec(string name, float radius, float angle, Vector2 size, bool playground)
        {
            this.name = name;
            this.radius = radius;
            this.angle = angle;
            this.size = size;
            this.playground = playground;
        }
    }

    private static readonly RingSpec[] DistrictRings =
    {
        new RingSpec(110f, 18, 8f, 0.90f, 1.12f, 0f),
        new RingSpec(170f, 26, 3f, 0.95f, 1.28f, 0f),
        new RingSpec(245f, 34, 6f, 1.00f, 1.45f, 0f),
        new RingSpec(330f, 42, 1f, 1.00f, 1.62f, 0f),
        new RingSpec(425f, 50, 4f, 1.10f, 1.82f, 0f),
        new RingSpec(525f, 60, 2f, 1.20f, 2.05f, 0f)
    };

    private static readonly RingSpec[] InfillRings =
    {
        new RingSpec(52f, 20, 9.0f, 0.55f, 0.72f, 0f),
        new RingSpec(82f, 26, 4.0f, 0.58f, 0.78f, 0f),
        new RingSpec(112f, 30, 6.0f, 0.62f, 0.82f, 0f),
        new RingSpec(172f, 38, 4.7f, 0.64f, 0.86f, 0f),
        new RingSpec(247f, 48, 3.8f, 0.66f, 0.90f, 0f),
        new RingSpec(332f, 58, 3.1f, 0.68f, 0.94f, 0f),
        new RingSpec(427f, 68, 2.7f, 0.70f, 0.98f, 0f),
        new RingSpec(527f, 76, 2.4f, 0.72f, 1.02f, 0f)
    };

    private static readonly OpenSpaceSpec[] OpenSpaces =
    {
        new OpenSpaceSpec("Archive Workers Memorial Park", 152f, 22.5f, new Vector2(34f, 25f), false),
        new OpenSpaceSpec("Forgotten Children Playground", 195f, 112.5f, new Vector2(31f, 25f), true),
        new OpenSpaceSpec("Collapsed Library Garden", 275f, 202.5f, new Vector2(38f, 28f), false),
        new OpenSpaceSpec("Memory Rail Playground", 360f, 292.5f, new Vector2(34f, 27f), true),
        new OpenSpaceSpec("Civic Pocket Park", 455f, 67.5f, new Vector2(40f, 29f), false),
        new OpenSpaceSpec("Outer District Play Lot", 475f, 247.5f, new Vector2(36f, 28f), true)
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
        Material concreteMaterial = ResolveNamedMaterial(lowrise, "MR3D_Building", roadMaterial);
        Material darkMaterial = ResolveNamedMaterial(lowrise, "MR3D_DarkPanel", concreteMaterial);
        Material glowMaterial = ResolveNamedMaterial(archive, "MR3D_MemoryGlow", darkMaterial);
        CreateExpandedGround(backdrop.transform, roadMaterial);
        ConfigureExpandedBoundary();
        ConfigureCityVisibility();

        List<Footprint> occupied = CollectExistingBuildingFootprints(backdrop.transform);
        int parkCount;
        int playgroundCount;
        CreateOpenSpaces(
            backdrop.transform,
            occupied,
            lamp,
            roadMaterial,
            concreteMaterial,
            darkMaterial,
            glowMaterial,
            out parkCount,
            out playgroundCount);
        int rejectedForegroundPlacements;
        int foregroundBuildingCount = CreateForegroundStreetBuildings(
            backdrop.transform,
            lowrise,
            midrise,
            tower,
            concreteMaterial,
            darkMaterial,
            glowMaterial,
            occupied,
            buildingBlockers,
            out rejectedForegroundPlacements);
        int rejectedPlacements;
        int primaryBuildingCount = CreateDistrictBuildings(
            backdrop.transform,
            lowrise,
            midrise,
            tower,
            concreteMaterial,
            darkMaterial,
            glowMaterial,
            occupied,
            buildingBlockers,
            out rejectedPlacements);
        int rejectedInfillPlacements;
        int infillBuildingCount = CreateInfillBuildings(
            backdrop.transform,
            lowrise,
            midrise,
            tower,
            concreteMaterial,
            darkMaterial,
            glowMaterial,
            occupied,
            buildingBlockers,
            out rejectedInfillPlacements);
        int buildingCount = foregroundBuildingCount + primaryBuildingCount + infillBuildingCount;
        rejectedPlacements += rejectedForegroundPlacements + rejectedInfillPlacements;
        int lampCount = CreateDistrictLamps(backdrop.transform, lamp);
        int roadCount = CreateRingRoads(backdrop.transform, roadMaterial);
        int radialAvenueCount = CreateRadialAvenues(backdrop.transform, roadMaterial);

        Physics.SyncTransforms();
        int colliderOverlapCount = CountColliderOverlaps(buildingBlockers);
        int relocatedMemoryOrbCount = RepositionMemoryOrbsOutsideBuildings(buildingBlockers);
        RescuePlayerIfInsideBuilding(buildingBlockers);

        if (colliderOverlapCount > 0)
            Debug.LogError($"Archive city collider spacing audit failed: overlaps={colliderOverlapCount}.");

        Debug.Log(
            $"Expanded archive city loaded: buildings={buildingCount}, foregroundBuildings={foregroundBuildingCount}, " +
            $"infillBuildings={infillBuildingCount}, lamps={lampCount}, " +
            $"buildingArchetypes=10, parks={parkCount}, playgrounds={playgroundCount}, " +
            $"ringRoads={roadCount}, radialAvenues={radialAvenueCount}, " +
            $"buildingColliders={buildingBlockers.Count}, " +
            $"coreColliders={coreColliderCount}, rejectedOverlaps={rejectedPlacements}, " +
            $"relocatedCoreBuildings={relocatedCoreBuildingCount}, " +
            $"colliderOverlaps={colliderOverlapCount}, " +
            $"relocatedMemoryOrbs={relocatedMemoryOrbCount}, " +
            $"boundaryRadius={ExpandedBoundaryRadius:0}m, cityDiameter={OuterCityRadius * 2f:0}m.");
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

    private static void CreateOpenSpaces(
        Transform parent,
        List<Footprint> occupied,
        Transform lampTemplate,
        Material groundMaterial,
        Material concreteMaterial,
        Material darkMaterial,
        Material glowMaterial,
        out int parkCount,
        out int playgroundCount)
    {
        GameObject root = new GameObject("Ruined Civic Open Spaces");
        root.transform.SetParent(parent, false);
        parkCount = 0;
        playgroundCount = 0;

        for (int i = 0; i < OpenSpaces.Length; i++)
        {
            OpenSpaceSpec spec = OpenSpaces[i];
            float footprintRadius = spec.size.magnitude * 0.5f + 5f;
            if (!TryFindOpenSpacePosition(spec, footprintRadius, occupied, out Vector3 position, out float angle))
            {
                Debug.LogWarning($"Skipped open space '{spec.name}' because no clear block was available.");
                continue;
            }

            GameObject space = new GameObject(spec.name);
            space.transform.SetParent(root.transform, false);
            space.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, angle + 90f, 0f));

            CreateOpenSpaceFoundation(space.transform, spec.size, groundMaterial, concreteMaterial);
            if (spec.playground)
            {
                CreateRuinedPlayground(space.transform, spec.size, concreteMaterial, darkMaterial, glowMaterial);
                playgroundCount++;
            }
            else
            {
                CreatePocketPark(space.transform, spec.size, concreteMaterial, darkMaterial, glowMaterial, i);
                parkCount++;
            }

            AddOpenSpaceLamps(space.transform, lampTemplate, spec.size);
            occupied.Add(new Footprint(position, footprintRadius));
        }
    }

    private static bool TryFindOpenSpacePosition(
        OpenSpaceSpec spec,
        float footprintRadius,
        List<Footprint> occupied,
        out Vector3 position,
        out float angle)
    {
        for (int attempt = 0; attempt < 14; attempt++)
        {
            int direction = attempt % 2 == 0 ? -1 : 1;
            float angularOffset = attempt == 0 ? 0f : direction * ((attempt + 1) / 2) * 3.5f;
            float radialOffset = (attempt / 5) * 14f;
            angle = spec.angle + angularOffset;
            position = ArchiveCenter + DirectionFromAngle(angle) * (spec.radius + radialOffset);
            if (!OverlapsAny(position, footprintRadius, occupied))
                return true;
        }

        position = Vector3.zero;
        angle = spec.angle;
        return false;
    }

    private static void CreateOpenSpaceFoundation(
        Transform parent,
        Vector2 size,
        Material groundMaterial,
        Material curbMaterial)
    {
        CreatePrimitiveProp(
            PrimitiveType.Cube,
            parent,
            "Cracked Public Space Surface",
            new Vector3(0f, 0.08f, 0f),
            new Vector3(size.x, 0.16f, size.y),
            Quaternion.identity,
            groundMaterial,
            true);

        float halfX = size.x * 0.5f;
        float halfZ = size.y * 0.5f;
        CreatePrimitiveProp(PrimitiveType.Cube, parent, "Curb North", new Vector3(0f, 0.22f, halfZ), new Vector3(size.x, 0.32f, 0.42f), Quaternion.identity, curbMaterial, true);
        CreatePrimitiveProp(PrimitiveType.Cube, parent, "Curb South", new Vector3(0f, 0.22f, -halfZ), new Vector3(size.x, 0.32f, 0.42f), Quaternion.identity, curbMaterial, true);
        CreatePrimitiveProp(PrimitiveType.Cube, parent, "Curb East", new Vector3(halfX, 0.22f, 0f), new Vector3(0.42f, 0.32f, size.y), Quaternion.identity, curbMaterial, true);
        CreatePrimitiveProp(PrimitiveType.Cube, parent, "Curb West", new Vector3(-halfX, 0.22f, 0f), new Vector3(0.42f, 0.32f, size.y), Quaternion.identity, curbMaterial, true);
    }

    private static void CreatePocketPark(
        Transform parent,
        Vector2 size,
        Material concreteMaterial,
        Material darkMaterial,
        Material glowMaterial,
        int parkIndex)
    {
        float treeX = size.x * 0.30f;
        float treeZ = size.y * 0.28f;
        CreateDeadTree(parent, new Vector3(-treeX, 0.2f, -treeZ), darkMaterial, -12f);
        CreateDeadTree(parent, new Vector3(treeX, 0.2f, treeZ), darkMaterial, 9f);
        CreateDeadTree(parent, new Vector3(-treeX, 0.2f, treeZ), darkMaterial, 17f);

        CreatePrimitiveProp(
            PrimitiveType.Cube,
            parent,
            "Broken Memorial Plinth",
            new Vector3(0f, 0.70f, 0f),
            new Vector3(3.2f, 1.25f, 3.2f),
            Quaternion.Euler(0f, parkIndex * 13f, parkIndex % 2 == 0 ? 0f : 3f),
            concreteMaterial,
            true);
        CreatePrimitiveProp(
            PrimitiveType.Cylinder,
            parent,
            "Memory Park Beacon",
            new Vector3(0f, 2.15f, 0f),
            new Vector3(0.34f, 0.95f, 0.34f),
            Quaternion.identity,
            glowMaterial,
            false);

        CreateBrokenBench(parent, new Vector3(-5.2f, 0.65f, 2.8f), -18f, concreteMaterial, darkMaterial);
        CreateBrokenBench(parent, new Vector3(5.6f, 0.65f, -3.0f), 164f, concreteMaterial, darkMaterial);
    }

    private static void CreateRuinedPlayground(
        Transform parent,
        Vector2 size,
        Material concreteMaterial,
        Material darkMaterial,
        Material glowMaterial)
    {
        float swingX = -size.x * 0.22f;
        float swingZ = 0f;
        float postHalfWidth = 2.8f;
        CreatePrimitiveProp(PrimitiveType.Cube, parent, "Swing Post FL", new Vector3(swingX - postHalfWidth, 2.1f, swingZ - 1.3f), new Vector3(0.28f, 4.0f, 0.28f), Quaternion.Euler(0f, 0f, -5f), darkMaterial, true);
        CreatePrimitiveProp(PrimitiveType.Cube, parent, "Swing Post FR", new Vector3(swingX + postHalfWidth, 2.1f, swingZ - 1.3f), new Vector3(0.28f, 4.0f, 0.28f), Quaternion.Euler(0f, 0f, 4f), darkMaterial, true);
        CreatePrimitiveProp(PrimitiveType.Cube, parent, "Swing Post BL", new Vector3(swingX - postHalfWidth, 2.1f, swingZ + 1.3f), new Vector3(0.28f, 4.0f, 0.28f), Quaternion.Euler(0f, 0f, -5f), darkMaterial, true);
        CreatePrimitiveProp(PrimitiveType.Cube, parent, "Swing Post BR", new Vector3(swingX + postHalfWidth, 2.1f, swingZ + 1.3f), new Vector3(0.28f, 4.0f, 0.28f), Quaternion.Euler(0f, 0f, 4f), darkMaterial, true);
        CreatePrimitiveProp(PrimitiveType.Cube, parent, "Bent Swing Bar", new Vector3(swingX, 4.05f, swingZ), new Vector3(6.2f, 0.30f, 0.30f), Quaternion.Euler(0f, 0f, -2f), darkMaterial, true);

        for (int seat = 0; seat < 2; seat++)
        {
            float x = swingX + (seat == 0 ? -1.45f : 1.35f);
            float tilt = seat == 0 ? -7f : 13f;
            CreatePrimitiveProp(PrimitiveType.Cylinder, parent, "Swing Chain L", new Vector3(x - 0.45f, 2.55f, swingZ), new Vector3(0.045f, 1.35f, 0.045f), Quaternion.Euler(0f, 0f, tilt), darkMaterial, false);
            CreatePrimitiveProp(PrimitiveType.Cylinder, parent, "Swing Chain R", new Vector3(x + 0.45f, 2.55f, swingZ), new Vector3(0.045f, 1.35f, 0.045f), Quaternion.Euler(0f, 0f, tilt), darkMaterial, false);
            CreatePrimitiveProp(PrimitiveType.Cube, parent, "Damaged Swing Seat", new Vector3(x, 1.25f, swingZ), new Vector3(1.25f, 0.16f, 0.55f), Quaternion.Euler(tilt * 0.4f, 0f, tilt), concreteMaterial, true);
        }

        float slideX = size.x * 0.22f;
        CreatePrimitiveProp(PrimitiveType.Cube, parent, "Slide Platform", new Vector3(slideX, 2.4f, 1.7f), new Vector3(3.2f, 0.30f, 3.0f), Quaternion.identity, concreteMaterial, true);
        CreatePrimitiveProp(PrimitiveType.Cube, parent, "Broken Slide Ramp", new Vector3(slideX, 1.25f, -1.2f), new Vector3(2.0f, 0.28f, 6.2f), Quaternion.Euler(-20f, 0f, 2f), concreteMaterial, true);
        CreatePrimitiveProp(PrimitiveType.Cube, parent, "Slide Rail L", new Vector3(slideX - 1.0f, 1.65f, -1.1f), new Vector3(0.16f, 0.18f, 6.0f), Quaternion.Euler(-20f, 0f, 2f), darkMaterial, false);
        CreatePrimitiveProp(PrimitiveType.Cube, parent, "Slide Rail R", new Vector3(slideX + 1.0f, 1.65f, -1.1f), new Vector3(0.16f, 0.18f, 6.0f), Quaternion.Euler(-20f, 0f, 2f), darkMaterial, false);
        CreatePrimitiveProp(PrimitiveType.Cylinder, parent, "Playground Memory Beacon", new Vector3(0f, 1.2f, size.y * 0.28f), new Vector3(0.32f, 1.0f, 0.32f), Quaternion.identity, glowMaterial, false);
    }

    private static void CreateDeadTree(Transform parent, Vector3 position, Material material, float lean)
    {
        CreatePrimitiveProp(PrimitiveType.Cylinder, parent, "Dead Tree Trunk", position + Vector3.up * 2.35f, new Vector3(0.42f, 2.25f, 0.42f), Quaternion.Euler(lean * 0.25f, 0f, lean), material, true);
        CreatePrimitiveProp(PrimitiveType.Cube, parent, "Dead Branch A", position + new Vector3(0.7f, 4.25f, 0f), new Vector3(2.1f, 0.18f, 0.18f), Quaternion.Euler(0f, 25f, 28f + lean), material, false);
        CreatePrimitiveProp(PrimitiveType.Cube, parent, "Dead Branch B", position + new Vector3(-0.55f, 3.75f, 0.25f), new Vector3(1.7f, 0.16f, 0.16f), Quaternion.Euler(0f, -35f, -32f + lean), material, false);
    }

    private static void CreateBrokenBench(
        Transform parent,
        Vector3 position,
        float yaw,
        Material concreteMaterial,
        Material darkMaterial)
    {
        Quaternion rotation = Quaternion.Euler(0f, yaw, 0f);
        CreatePrimitiveProp(PrimitiveType.Cube, parent, "Broken Park Bench Seat", position, new Vector3(3.8f, 0.24f, 0.75f), rotation * Quaternion.Euler(0f, 0f, 3f), concreteMaterial, true);
        CreatePrimitiveProp(PrimitiveType.Cube, parent, "Bench Leg L", position + rotation * new Vector3(-1.3f, -0.45f, 0f), new Vector3(0.22f, 0.90f, 0.62f), rotation, darkMaterial, true);
        CreatePrimitiveProp(PrimitiveType.Cube, parent, "Bench Leg R", position + rotation * new Vector3(1.3f, -0.42f, 0f), new Vector3(0.22f, 0.84f, 0.62f), rotation * Quaternion.Euler(0f, 0f, -9f), darkMaterial, true);
    }

    private static void AddOpenSpaceLamps(Transform parent, Transform lampTemplate, Vector2 size)
    {
        Vector3[] positions =
        {
            new Vector3(-size.x * 0.38f, 0f, -size.y * 0.37f),
            new Vector3(size.x * 0.38f, 0f, size.y * 0.37f)
        };

        for (int i = 0; i < positions.Length; i++)
        {
            Transform lamp = Object.Instantiate(lampTemplate.gameObject, parent).transform;
            lamp.name = $"Open Space Lamp {i + 1}";
            lamp.gameObject.SetActive(true);
            lamp.localPosition = positions[i];
            lamp.localRotation = Quaternion.Euler(0f, i == 0 ? 35f : 215f, 0f);
            lamp.localScale = Vector3.one * 0.92f;
            PrepareDistantVisual(lamp.gameObject, true);
            AddLampCollider(lamp);
        }
    }

    private static GameObject CreatePrimitiveProp(
        PrimitiveType primitiveType,
        Transform parent,
        string objectName,
        Vector3 localPosition,
        Vector3 localScale,
        Quaternion localRotation,
        Material material,
        bool collidable)
    {
        GameObject prop = GameObject.CreatePrimitive(primitiveType);
        prop.name = objectName;
        prop.transform.SetParent(parent, false);
        prop.transform.localPosition = localPosition;
        prop.transform.localRotation = localRotation;
        prop.transform.localScale = localScale;

        Renderer renderer = prop.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = true;
        }

        Collider collider = prop.GetComponent<Collider>();
        if (collider != null)
            collider.enabled = collidable;

        return prop;
    }

    private static int CreateDistrictBuildings(
        Transform parent,
        Transform lowrise,
        Transform midrise,
        Transform tower,
        Material concreteMaterial,
        Material darkMaterial,
        Material glowMaterial,
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

                int archetype = SelectBuildingArchetype(ringIndex, i);
                float scale = Mathf.Lerp(ring.minScale, ring.maxScale, Hash01(ringIndex, i, 3));
                if (ringIndex >= DistrictRings.Length - 2 && (archetype == 2 || archetype >= 8))
                    scale *= 1.08f;

                float footprintRadius = GetArchetypeFootprintRadius(archetype, templateRadii) * scale;
                if (!TryFindClearPosition(ringIndex, i, ring, footprintRadius, occupied, out Vector3 position, out float angle))
                {
                    rejectedPlacements++;
                    continue;
                }

                Transform instance = CreateBuildingArchetype(
                    ringRoot.transform,
                    templates,
                    archetype,
                    $"Backdrop Building R{ringIndex + 1}_{i:00}_A{archetype:00}",
                    concreteMaterial,
                    darkMaterial,
                    glowMaterial,
                    Hash01(ringIndex, i, 9));
                instance.SetPositionAndRotation(position, Quaternion.Euler(0f, angle + 90f, 0f));
                instance.localScale = Vector3.one * scale;

                // Composite archetypes can exceed their conservative template estimate.
                // Recheck the completed visual bounds before accepting the block.
                float actualFootprintRadius = GetWorldFootprintRadius(instance);
                footprintRadius = Mathf.Max(footprintRadius, actualFootprintRadius);
                if (IsAvenueClearZone(position, footprintRadius) || OverlapsAny(position, footprintRadius, occupied))
                {
                    if (!TryFindClearPosition(
                            ringIndex,
                            i,
                            ring,
                            footprintRadius,
                            occupied,
                            out position,
                            out angle))
                    {
                        instance.gameObject.SetActive(false);
                        Object.Destroy(instance.gameObject);
                        rejectedPlacements++;
                        continue;
                    }

                    instance.SetPositionAndRotation(position, Quaternion.Euler(0f, angle + 90f, 0f));
                }

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

    private static int CreateForegroundStreetBuildings(
        Transform parent,
        Transform lowrise,
        Transform midrise,
        Transform tower,
        Material concreteMaterial,
        Material darkMaterial,
        Material glowMaterial,
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
        float[] innerStreetZ = { -50f, -28f, -6f, 16f, 58f, 80f, 102f, 124f };
        float[] outerStreetZ = { -39f, -12f, 69f, 96f, 123f };
        GameObject streetRoot = new GameObject("Dense Foreground Street Blocks");
        streetRoot.transform.SetParent(parent, false);
        int created = 0;
        rejectedPlacements = 0;

        for (int side = -1; side <= 1; side += 2)
        {
            for (int i = 0; i < innerStreetZ.Length; i++)
            {
                float x = side * (26f + (i % 2) * 4f);
                Vector3 position = new Vector3(x, 0f, innerStreetZ[i]);
                int archetype = i % 3 == 0 ? 1 : i % 3 == 1 ? 0 : 7;
                float scale = Mathf.Lerp(0.66f, 0.86f, Hash01(side + 210, i, 4));
                if (TryCreateForegroundBuilding(
                        streetRoot.transform,
                        templates,
                        templateRadii,
                        archetype,
                        $"Near Street Building {side}_{i:00}",
                        position,
                        side < 0 ? 90f : -90f,
                        scale,
                        concreteMaterial,
                        darkMaterial,
                        glowMaterial,
                        occupied,
                        blockers,
                        side + 210,
                        i))
                    created++;
                else
                    rejectedPlacements++;
            }

            for (int i = 0; i < outerStreetZ.Length; i++)
            {
                Vector3 position = new Vector3(side * 49f, 0f, outerStreetZ[i]);
                int archetype = i % 2 == 0 ? 1 : 0;
                float scale = Mathf.Lerp(0.74f, 0.96f, Hash01(side + 240, i, 5));
                if (TryCreateForegroundBuilding(
                        streetRoot.transform,
                        templates,
                        templateRadii,
                        archetype,
                        $"Outer Street Building {side}_{i:00}",
                        position,
                        side < 0 ? 90f : -90f,
                        scale,
                        concreteMaterial,
                        darkMaterial,
                        glowMaterial,
                        occupied,
                        blockers,
                        side + 240,
                        i))
                    created++;
                else
                    rejectedPlacements++;
            }
        }

        return created;
    }

    private static bool TryCreateForegroundBuilding(
        Transform parent,
        Transform[] templates,
        float[] templateRadii,
        int archetype,
        string objectName,
        Vector3 position,
        float yaw,
        float scale,
        Material concreteMaterial,
        Material darkMaterial,
        Material glowMaterial,
        List<Footprint> occupied,
        List<Collider> blockers,
        int hashGroup,
        int hashIndex)
    {
        float footprintRadius = GetArchetypeFootprintRadius(archetype, templateRadii) * scale;
        if (Mathf.Abs(position.x) < PrimaryAvenueHalfWidth + footprintRadius ||
            OverlapsAny(position, footprintRadius, occupied))
            return false;

        Transform instance = CreateBuildingArchetype(
            parent,
            templates,
            archetype,
            objectName,
            concreteMaterial,
            darkMaterial,
            glowMaterial,
            Hash01(hashGroup, hashIndex, 9));
        instance.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
        instance.localScale = Vector3.one * scale;

        footprintRadius = Mathf.Max(footprintRadius, GetWorldFootprintRadius(instance));
        if (OverlapsAny(position, footprintRadius, occupied))
        {
            instance.gameObject.SetActive(false);
            Object.Destroy(instance.gameObject);
            return false;
        }

        PrepareDistantVisual(instance.gameObject, false);
        BoxCollider collider = AddBuildingCollider(instance);
        if (collider != null)
        {
            if (OverlapsExistingBlocker(collider, blockers))
            {
                collider.enabled = false;
                instance.gameObject.SetActive(false);
                Object.Destroy(instance.gameObject);
                return false;
            }

            blockers.Add(collider);
        }

        occupied.Add(new Footprint(position, footprintRadius));
        return true;
    }

    private static int CreateInfillBuildings(
        Transform parent,
        Transform lowrise,
        Transform midrise,
        Transform tower,
        Material concreteMaterial,
        Material darkMaterial,
        Material glowMaterial,
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
        GameObject infillRoot = new GameObject("Dense Ruined Block Infill");
        infillRoot.transform.SetParent(parent, false);
        int created = 0;
        rejectedPlacements = 0;

        for (int ringIndex = 0; ringIndex < InfillRings.Length; ringIndex++)
        {
            RingSpec ring = InfillRings[ringIndex];
            GameObject ringRoot = new GameObject($"Infill District Ring {ringIndex + 1}");
            ringRoot.transform.SetParent(infillRoot.transform, false);

            for (int i = 0; i < ring.count; i++)
            {
                int hashRingIndex = ringIndex + 100;
                int archetype = SelectInfillBuildingArchetype(ringIndex, i);
                float scale = Mathf.Lerp(ring.minScale, ring.maxScale, Hash01(hashRingIndex, i, 3));
                float footprintRadius = GetArchetypeFootprintRadius(archetype, templateRadii) * scale;
                if (!TryFindClearPosition(
                        hashRingIndex,
                        i,
                        ring,
                        footprintRadius,
                        occupied,
                        out Vector3 position,
                        out float angle))
                {
                    rejectedPlacements++;
                    continue;
                }

                Transform instance = CreateBuildingArchetype(
                    ringRoot.transform,
                    templates,
                    archetype,
                    $"Infill Building R{ringIndex + 1}_{i:00}_A{archetype:00}",
                    concreteMaterial,
                    darkMaterial,
                    glowMaterial,
                    Hash01(hashRingIndex, i, 9));
                instance.SetPositionAndRotation(position, Quaternion.Euler(0f, angle + 90f, 0f));
                instance.localScale = Vector3.one * scale;

                float actualFootprintRadius = GetWorldFootprintRadius(instance);
                footprintRadius = Mathf.Max(footprintRadius, actualFootprintRadius);
                if (IsAvenueClearZone(position, footprintRadius) || OverlapsAny(position, footprintRadius, occupied))
                {
                    if (!TryFindClearPosition(
                            hashRingIndex,
                            i,
                            ring,
                            footprintRadius,
                            occupied,
                            out position,
                            out angle))
                    {
                        instance.gameObject.SetActive(false);
                        Object.Destroy(instance.gameObject);
                        rejectedPlacements++;
                        continue;
                    }

                    instance.SetPositionAndRotation(position, Quaternion.Euler(0f, angle + 90f, 0f));
                }

                PrepareDistantVisual(instance.gameObject, true);
                BoxCollider collider = AddBuildingCollider(instance);
                if (collider != null)
                {
                    if (OverlapsExistingBlocker(collider, blockers))
                    {
                        collider.enabled = false;
                        instance.gameObject.SetActive(false);
                        Object.Destroy(instance.gameObject);
                        rejectedPlacements++;
                        continue;
                    }

                    blockers.Add(collider);
                }

                occupied.Add(new Footprint(position, footprintRadius));
                created++;
            }
        }

        return created;
    }

    private static int SelectInfillBuildingArchetype(int ringIndex, int itemIndex)
    {
        int selector = Mathf.FloorToInt(Hash01(ringIndex + 100, itemIndex, 12) * 1000f);
        if (ringIndex < 2)
        {
            switch (selector % 3) { case 0: return 0; case 1: return 1; default: return 7; }
        }

        switch (selector % 4) { case 0: return 0; case 1: return 1; case 2: return 5; default: return 7; }
    }

    private static Transform CreateBuildingArchetype(
        Transform parent,
        Transform[] templates,
        int archetype,
        string objectName,
        Material concreteMaterial,
        Material darkMaterial,
        Material glowMaterial,
        float detailHash)
    {
        GameObject container = new GameObject(objectName);
        container.transform.SetParent(parent, false);

        switch (archetype)
        {
            case 0: // Compact street retail / low-rise housing.
                AddBuildingPiece(templates[0], container.transform, "Retail Shell", Vector3.zero, Vector3.one, 0f);
                break;
            case 1: // Narrow office block.
                AddBuildingPiece(templates[1], container.transform, "Office Block", Vector3.zero, new Vector3(1.15f, 1.28f, 1.00f), 0f);
                break;
            case 2: // Tall damaged tower.
                AddBuildingPiece(templates[2], container.transform, "Highrise Tower", Vector3.zero, new Vector3(1.04f, 1.38f, 1.04f), 0f);
                break;
            case 3: // Residential slab pair.
                AddBuildingPiece(templates[1], container.transform, "Apartment Slab A", new Vector3(-5.1f, 0f, 0f), new Vector3(0.88f, 1.35f, 0.92f), -3f);
                AddBuildingPiece(templates[1], container.transform, "Apartment Slab B", new Vector3(5.1f, 0f, 1.4f), new Vector3(0.88f, 1.12f, 0.92f), 4f);
                break;
            case 4: // Shopping or transit podium with a rear office volume.
                AddBuildingPiece(templates[0], container.transform, "Commercial Podium", Vector3.zero, new Vector3(2.15f, 0.74f, 1.48f), 0f);
                AddBuildingPiece(templates[1], container.transform, "Podium Office", new Vector3(0f, 0f, 3.2f), new Vector3(0.76f, 0.92f, 0.72f), 0f);
                break;
            case 5: // Outer-district industrial hall.
                AddBuildingPiece(templates[0], container.transform, "Industrial Hall", Vector3.zero, new Vector3(2.60f, 0.62f, 1.72f), 0f);
                break;
            case 6: // Civic building with symmetrical wings.
                AddBuildingPiece(templates[1], container.transform, "Civic Core", Vector3.zero, new Vector3(1.22f, 1.18f, 1.08f), 0f);
                AddBuildingPiece(templates[0], container.transform, "Civic Wing L", new Vector3(-10.0f, 0f, 0.8f), new Vector3(0.78f, 0.72f, 0.90f), 0f);
                AddBuildingPiece(templates[0], container.transform, "Civic Wing R", new Vector3(10.0f, 0f, 0.8f), new Vector3(0.78f, 0.72f, 0.90f), 0f);
                break;
            case 7: // Broad parking or utility deck.
                AddBuildingPiece(templates[0], container.transform, "Parking Deck", Vector3.zero, new Vector3(1.90f, 0.52f, 1.45f), 0f);
                break;
            case 8: // Asymmetric metropolitan tower cluster.
                AddBuildingPiece(templates[2], container.transform, "Cluster Tower", new Vector3(-4.2f, 0f, 0f), new Vector3(0.92f, 1.42f, 0.92f), -2f);
                AddBuildingPiece(templates[1], container.transform, "Cluster Annex", new Vector3(6.3f, 0f, 2.1f), new Vector3(0.92f, 1.12f, 0.82f), 5f);
                break;
            default: // Fractured megablock landmark.
                AddBuildingPiece(templates[2], container.transform, "Megablock Tower", Vector3.zero, new Vector3(1.16f, 1.62f, 1.05f), 0f);
                AddBuildingPiece(templates[0], container.transform, "Megablock Base", new Vector3(0f, 0f, 5.2f), new Vector3(1.65f, 0.70f, 1.30f), 0f);
                break;
        }

        AddRooflineDetails(container.transform, archetype, concreteMaterial, darkMaterial, glowMaterial, detailHash);
        return container.transform;
    }

    private static Transform AddBuildingPiece(
        Transform template,
        Transform parent,
        string pieceName,
        Vector3 localPosition,
        Vector3 localScale,
        float localYaw)
    {
        Transform piece = Object.Instantiate(template.gameObject, parent).transform;
        piece.name = pieceName;
        piece.gameObject.SetActive(true);
        piece.localPosition = localPosition;
        piece.localRotation = Quaternion.Euler(0f, localYaw, 0f);
        piece.localScale = localScale;

        foreach (Collider collider in piece.GetComponentsInChildren<Collider>(true))
            collider.enabled = false;

        return piece;
    }

    private static void AddRooflineDetails(
        Transform building,
        int archetype,
        Material concreteMaterial,
        Material darkMaterial,
        Material glowMaterial,
        float detailHash)
    {
        if (!TryGetLocalVisualBounds(building, out Bounds bounds))
            return;

        if (detailHash > 0.22f)
        {
            CreatePrimitiveProp(
                PrimitiveType.Cube,
                building,
                "Broken Rooftop Sign",
                new Vector3(bounds.center.x, bounds.max.y + 1.35f, bounds.center.z),
                new Vector3(Mathf.Lerp(2.6f, 4.8f, detailHash), 2.2f, 0.28f),
                Quaternion.Euler(0f, archetype % 2 == 0 ? 0f : 90f, archetype % 3 == 0 ? 7f : -4f),
                detailHash > 0.72f ? glowMaterial : darkMaterial,
                false);
        }

        if (detailHash < 0.78f)
        {
            CreatePrimitiveProp(
                PrimitiveType.Cylinder,
                building,
                "Rooftop Water Tank",
                new Vector3(bounds.center.x + bounds.extents.x * 0.32f, bounds.max.y + 0.85f, bounds.center.z),
                new Vector3(1.25f, 0.78f, 1.25f),
                Quaternion.identity,
                concreteMaterial,
                false);
        }
    }

    private static float GetArchetypeFootprintRadius(int archetype, float[] templateRadii)
    {
        switch (archetype)
        {
            case 0: return templateRadii[0];
            case 1: return templateRadii[1] * 1.25f;
            case 2: return templateRadii[2] * 1.12f;
            case 3: return templateRadii[1] * 1.65f;
            case 4: return Mathf.Max(templateRadii[0] * 2.20f, templateRadii[1]);
            case 5: return templateRadii[0] * 2.65f;
            case 6: return Mathf.Max(templateRadii[1] * 1.30f, templateRadii[0] * 2.75f);
            case 7: return templateRadii[0] * 1.95f;
            case 8: return Mathf.Max(templateRadii[2] * 1.35f, templateRadii[1] * 1.75f);
            default: return Mathf.Max(templateRadii[2] * 1.25f, templateRadii[0] * 1.80f);
        }
    }

    private static int SelectBuildingArchetype(int ringIndex, int itemIndex)
    {
        int selector = Mathf.FloorToInt(Hash01(ringIndex, itemIndex, 12) * 1000f);
        switch (ringIndex)
        {
            case 0:
                switch (selector % 4) { case 0: return 0; case 1: return 4; case 2: return 6; default: return 7; }
            case 1:
                switch (selector % 5) { case 0: return 0; case 1: return 1; case 2: return 3; case 3: return 4; default: return 7; }
            case 2:
                switch (selector % 5) { case 0: return 1; case 1: return 3; case 2: return 4; case 3: return 6; default: return 8; }
            case 3:
                switch (selector % 6) { case 0: return 1; case 1: return 2; case 2: return 3; case 3: return 4; case 4: return 8; default: return 9; }
            case 4:
                switch (selector % 5) { case 0: return 2; case 1: return 3; case 2: return 5; case 3: return 8; default: return 9; }
            default:
                switch (selector % 5) { case 0: return 2; case 1: return 5; case 2: return 8; case 3: return 9; default: return 1; }
        }
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

        for (int attempt = 0; attempt < 20; attempt++)
        {
            int direction = attempt % 2 == 0 ? -1 : 1;
            float angularOffset = attempt == 0 ? 0f : direction * ((attempt + 1) / 2) * 2.35f;
            float outwardOffset = (attempt / 6) * 7.5f;
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
        Vector2 relative = new Vector2(position.x - ArchiveCenter.x, position.z - ArchiveCenter.z);
        float radialDistance = relative.magnitude;
        if (radialDistance < InnerCivicClearRadius + footprintRadius)
            return true;

        if (radialDistance > OuterCityRadius + 36f)
            return false;

        for (int avenue = 0; avenue < RadialAvenueCount; avenue++)
        {
            float angle = avenue * (360f / RadialAvenueCount);
            Vector3 direction3D = DirectionFromAngle(angle);
            Vector2 direction = new Vector2(direction3D.x, direction3D.z);
            float along = Vector2.Dot(relative, direction);
            if (along < InnerCivicClearRadius - 4f)
                continue;

            float perpendicular = Mathf.Abs(relative.x * direction.y - relative.y * direction.x);
            bool primary = avenue % 2 == 0;
            float halfWidth = primary ? PrimaryAvenueHalfWidth : SecondaryAvenueHalfWidth;
            if (perpendicular < halfWidth + footprintRadius)
                return true;
        }

        return false;
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

        created += CreateLampRing(lampRoot.transform, lampTemplate, 74f, 18, 11f, 0.96f);
        created += CreateLampRing(lampRoot.transform, lampTemplate, 140f, 22, 4f, 1.00f);
        created += CreateLampRing(lampRoot.transform, lampTemplate, 210f, 28, 8f, 1.04f);
        created += CreateLampRing(lampRoot.transform, lampTemplate, 285f, 34, 2f, 1.08f);
        created += CreateLampRing(lampRoot.transform, lampTemplate, 375f, 40, 6f, 1.12f);
        created += CreateLampRing(lampRoot.transform, lampTemplate, 470f, 46, 3f, 1.16f);
        return created;
    }

    private static int CreateLampRing(Transform parent, Transform template, float radius, int count, float offset, float scale)
    {
        int created = 0;
        for (int i = 0; i < count; i++)
        {
            float angle = offset + i * (360f / count);
            Vector3 direction = DirectionFromAngle(angle);
            Transform lamp = Object.Instantiate(template.gameObject, parent).transform;
            lamp.name = $"Backdrop Street Lamp {radius:000}_{i:00}";
            lamp.position = ArchiveCenter + direction * radius;
            lamp.rotation = Quaternion.Euler(0f, angle + 90f, 0f);
            lamp.localScale = Vector3.one * scale;
            PrepareDistantVisual(lamp.gameObject, radius > 190f);
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

    private static void ConfigureCityVisibility()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera != null)
            mainCamera.farClipPlane = Mathf.Max(mainCamera.farClipPlane, 920f);

        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogStartDistance = 245f;
        RenderSettings.fogEndDistance = 860f;
    }

    private static int CreateRingRoads(Transform parent, Material roadMaterial)
    {
        if (roadMaterial == null)
            return 0;

        GameObject roadsRoot = new GameObject("Expanded Concentric Roads");
        roadsRoot.transform.SetParent(parent, false);

        CreateRingRoad(roadsRoot.transform, "Archive Civic Ring Road", 74f, 8.0f, roadMaterial);
        CreateRingRoad(roadsRoot.transform, "Inner Residential Ring Road", 140f, 9.0f, roadMaterial);
        CreateRingRoad(roadsRoot.transform, "Neighbourhood Ring Road", 210f, 10.0f, roadMaterial);
        CreateRingRoad(roadsRoot.transform, "Metropolitan Ring Road", 285f, 11.0f, roadMaterial);
        CreateRingRoad(roadsRoot.transform, "Business District Ring Road", 375f, 12.0f, roadMaterial);
        CreateRingRoad(roadsRoot.transform, "Outer District Ring Road", 470f, 13.0f, roadMaterial);
        CreateRingRoad(roadsRoot.transform, "Perimeter Ring Road", 570f, 14.0f, roadMaterial);
        return 7;
    }

    private static int CreateRadialAvenues(Transform parent, Material roadMaterial)
    {
        if (roadMaterial == null)
            return 0;

        GameObject avenuesRoot = new GameObject("Grand Radial Avenues");
        avenuesRoot.transform.SetParent(parent, false);
        for (int avenue = 0; avenue < RadialAvenueCount; avenue++)
        {
            float angle = avenue * (360f / RadialAvenueCount);
            Vector3 direction = DirectionFromAngle(angle);
            float width = avenue % 2 == 0 ? PrimaryAvenueHalfWidth * 2f : SecondaryAvenueHalfWidth * 2f;
            Vector3 start = ArchiveCenter + direction * 43f + Vector3.up * 0.042f;
            Vector3 end = ArchiveCenter + direction * 590f + Vector3.up * 0.042f;
            CreateStraightRoad(
                avenuesRoot.transform,
                $"Archive Radial Avenue {avenue + 1:00}",
                start,
                end,
                width,
                roadMaterial);
        }

        return RadialAvenueCount;
    }

    private static void CreateStraightRoad(
        Transform parent,
        string roadName,
        Vector3 start,
        Vector3 end,
        float width,
        Material material)
    {
        Vector3 forward = (end - start).normalized;
        Vector3 right = new Vector3(forward.z, 0f, -forward.x) * (width * 0.5f);
        Vector3[] vertices =
        {
            start - right,
            start + right,
            end - right,
            end + right
        };
        Vector2[] uv =
        {
            new Vector2(0f, 0f),
            new Vector2(1f, 0f),
            new Vector2(0f, Vector3.Distance(start, end) / 12f),
            new Vector2(1f, Vector3.Distance(start, end) / 12f)
        };
        int[] triangles = { 0, 2, 1, 1, 2, 3 };

        Mesh mesh = new Mesh { name = roadName + " Mesh" };
        mesh.vertices = vertices;
        mesh.uv = uv;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        GameObject road = new GameObject(roadName);
        road.transform.SetParent(parent, false);
        road.AddComponent<MeshFilter>().sharedMesh = mesh;
        MeshRenderer renderer = road.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = true;
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

    private static bool OverlapsExistingBlocker(Collider candidate, List<Collider> blockers)
    {
        if (candidate == null || !candidate.enabled)
            return false;

        for (int i = 0; i < blockers.Count; i++)
        {
            Collider existing = blockers[i];
            if (existing == null || !existing.enabled)
                continue;

            bool overlaps = Physics.ComputePenetration(
                candidate,
                candidate.transform.position,
                candidate.transform.rotation,
                existing,
                existing.transform.position,
                existing.transform.rotation,
                out _,
                out float distance);
            if (overlaps && distance > 0.02f)
                return true;
        }

        return false;
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

    private static Material ResolveNamedMaterial(Transform root, string materialName, Material fallback)
    {
        if (root == null)
            return fallback;

        foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
        {
            foreach (Material material in renderer.sharedMaterials)
            {
                if (material != null && material.name.StartsWith(materialName))
                    return material;
            }
        }

        return fallback;
    }

    private static float GetLocalFootprintRadius(Transform root)
    {
        if (!TryGetLocalVisualBounds(root, out Bounds bounds))
            return 8f;

        return Mathf.Sqrt(bounds.extents.x * bounds.extents.x + bounds.extents.z * bounds.extents.z);
    }

    private static float GetWorldFootprintRadius(Transform root)
    {
        if (!TryGetWorldVisualBounds(root, out Bounds bounds))
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
