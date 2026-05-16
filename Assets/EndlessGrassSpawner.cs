using System.Collections.Generic;
using UnityEngine;

public class EndlessGrassSpawner : MonoBehaviour
{
    private enum SegmentType
    {
        Flat,
        RiseStart,
        RiseContinue
    }

    [Header("References")]
    public Transform player;

    [Header("Generation")]
    public int initialExtraSegments = 8;
    public float spawnAheadDistance = 1800f;
    public float recycleBehindDistance = 1200f;
    public int maxSpawnPerFrame = 6;
    public int maxActiveSegments = 40;
    public float flatVisibleAdvance = 425f;
    [Range(0f, 1f)]
    public float riseChance = 0.3f;
    [Range(1, 3)]
    public int maxRiseSegments = 3;

    private readonly List<Transform> activeSegments = new List<Transform>();
    private Transform flatTemplate;
    private Transform riseStartTemplate;
    private Transform riseContinueTemplate;
    private Camera mainCamera;
    private Vector3 flatToRiseOffset;
    private Vector3 riseContinueOffset;
    private float flatWidth;
    private float baseFlatY;
    private float maxFlatY;
    private float currentFlatY;
    private int currentRiseSegments;
    private bool warnedInvalidGeneration;

    void Start()
    {
        if (player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
            {
                player = playerObject.transform;
            }
        }

        mainCamera = Camera.main;

        CacheTemplates();
        if (!HasAllTemplates())
        {
            enabled = false;
            return;
        }

        activeSegments.Add(flatTemplate);
        activeSegments.Add(riseStartTemplate);
        activeSegments.Add(riseContinueTemplate);
        activeSegments.Sort((a, b) => a.position.x.CompareTo(b.position.x));

        flatWidth = GetWidth(flatTemplate);
        flatToRiseOffset = riseStartTemplate.position - flatTemplate.position;
        riseContinueOffset = riseContinueTemplate.position - riseStartTemplate.position;
        baseFlatY = flatTemplate.position.y;
        maxFlatY = baseFlatY +
            flatToRiseOffset.y +
            riseContinueOffset.y * Mathf.Max(0, maxRiseSegments - 1);
        currentFlatY = riseContinueTemplate.position.y;
        currentRiseSegments = 2;

        if (flatWidth <= 0f ||
            flatToRiseOffset.x <= 0f ||
            riseContinueOffset.x <= 0f)
        {
            Debug.LogError("EndlessGrassSpawner needs positive segment widths and X offsets.");
            enabled = false;
            return;
        }

        for (int i = 0; i < initialExtraSegments; i++)
        {
            if (!TrySpawnNextSegment())
            {
                break;
            }
        }
    }

    void Update()
    {
        if (player == null || activeSegments.Count == 0)
        {
            return;
        }

        int spawnedThisFrame = 0;
        while (activeSegments.Count < maxActiveSegments &&
               spawnedThisFrame < maxSpawnPerFrame &&
               GetRightEdge(activeSegments[activeSegments.Count - 1]) < player.position.x + spawnAheadDistance)
        {
            if (!TrySpawnNextSegment())
            {
                break;
            }

            spawnedThisFrame++;
        }

        int cleanupCount = 0;
        while (activeSegments.Count > 0 &&
               cleanupCount < maxActiveSegments &&
               GetRightEdge(activeSegments[0]) < GetRecycleBoundary())
        {
            Transform oldSegment = activeSegments[0];
            activeSegments.RemoveAt(0);

            if (oldSegment != flatTemplate &&
                oldSegment != riseStartTemplate &&
                oldSegment != riseContinueTemplate)
            {
                Destroy(oldSegment.gameObject);
            }

            cleanupCount++;
        }
    }

    private void CacheTemplates()
    {
        GameObject[] groundObjects = GameObject.FindGameObjectsWithTag("Ground");
        foreach (GameObject groundObject in groundObjects)
        {
            string lowerName = groundObject.name.ToLowerInvariant();

            if (lowerName == "grass1")
            {
                flatTemplate = groundObject.transform;
            }
            else if (lowerName.StartsWith("grass2"))
            {
                riseStartTemplate = groundObject.transform;
            }
            else if (lowerName == "grass3")
            {
                riseContinueTemplate = groundObject.transform;
            }
        }
    }

    private bool HasAllTemplates()
    {
        return flatTemplate != null &&
               riseStartTemplate != null &&
               riseContinueTemplate != null;
    }

    private bool TrySpawnNextSegment()
    {
        Transform previous = activeSegments[activeSegments.Count - 1];
        float previousRightEdge = GetRightEdge(previous);
        SegmentType nextType = ChooseNextType();
        Transform template = GetTemplate(nextType);
        Transform clone = Instantiate(template);
        clone.name = template.name + "_Generated";

        switch (nextType)
        {
            case SegmentType.Flat:
                currentRiseSegments = 0;
                Vector2 targetLeftConnection = GetRightTopConnection(previous);
                Vector2 templateLeftConnection = GetLeftTopConnection(template);
                Vector2 connectionOffset = targetLeftConnection - templateLeftConnection;

                if (IsFlat(previous))
                {
                    connectionOffset.x = previous.position.x + flatVisibleAdvance - template.position.x;
                }

                clone.position = new Vector3(
                    template.position.x + connectionOffset.x,
                    template.position.y + connectionOffset.y,
                    template.position.z
                );
                currentFlatY = clone.position.y;
                break;

            case SegmentType.RiseStart:
                currentRiseSegments = 1;
                clone.position = previous.position + flatToRiseOffset;
                currentFlatY += flatToRiseOffset.y;
                break;

            case SegmentType.RiseContinue:
                currentRiseSegments++;
                clone.position = previous.position + riseContinueOffset;
                currentFlatY += riseContinueOffset.y;
                break;
        }

        float newRightEdge = GetRightEdge(clone);
        if (float.IsNaN(newRightEdge) ||
            float.IsInfinity(newRightEdge) ||
            newRightEdge <= previousRightEdge + 0.01f)
        {
            Destroy(clone.gameObject);

            if (!warnedInvalidGeneration)
            {
                warnedInvalidGeneration = true;
                Debug.LogError("EndlessGrassSpawner stopped because a generated segment did not advance.");
            }

            enabled = false;
            return false;
        }

        activeSegments.Add(clone);
        return true;
    }

    private SegmentType ChooseNextType()
    {
        if (currentRiseSegments > 0 && currentRiseSegments < maxRiseSegments)
        {
            return Random.value < 0.7f ? SegmentType.RiseContinue : SegmentType.Flat;
        }

        if (currentRiseSegments >= maxRiseSegments)
        {
            return SegmentType.Flat;
        }

        if (currentFlatY >= maxFlatY)
        {
            return SegmentType.Flat;
        }

        return Random.value < riseChance ? SegmentType.RiseStart : SegmentType.Flat;
    }

    private Transform GetTemplate(SegmentType type)
    {
        switch (type)
        {
            case SegmentType.RiseStart:
                return riseStartTemplate;
            case SegmentType.RiseContinue:
                return riseContinueTemplate;
            default:
                return flatTemplate;
        }
    }

    private bool IsFlat(Transform segment)
    {
        return segment != null &&
               segment.name.StartsWith(flatTemplate.name);
    }

    private float GetWidth(Transform segment)
    {
        Bounds? bounds = GetRenderedBounds(segment);
        return bounds.HasValue ? bounds.Value.size.x : 500f;
    }

    private float GetRightEdge(Transform segment)
    {
        Bounds? bounds = GetRenderedBounds(segment);
        return bounds.HasValue
            ? bounds.Value.max.x
            : segment.position.x + GetWidth(segment) * 0.5f;
    }

    private float GetLeftEdge(Transform segment)
    {
        Bounds? bounds = GetRenderedBounds(segment);
        return bounds.HasValue
            ? bounds.Value.min.x
            : segment.position.x - GetWidth(segment) * 0.5f;
    }

    private Vector2 GetLeftTopConnection(Transform segment)
    {
        return GetTopConnection(segment, true);
    }

    private Vector2 GetRightTopConnection(Transform segment)
    {
        return GetTopConnection(segment, false);
    }

    private Vector2 GetTopConnection(Transform segment, bool useLeftEdge)
    {
        SpriteRenderer[] renderers = segment.GetComponentsInChildren<SpriteRenderer>();
        bool found = false;
        Vector2 connection = Vector2.zero;

        foreach (SpriteRenderer renderer in renderers)
        {
            if (renderer.sprite == null)
            {
                continue;
            }

            Bounds spriteBounds = renderer.sprite.bounds;
            Vector3 localPoint = new Vector3(
                useLeftEdge ? spriteBounds.min.x : spriteBounds.max.x,
                spriteBounds.max.y,
                0f
            );
            Vector2 worldPoint = renderer.transform.TransformPoint(localPoint);

            if (!found ||
                (useLeftEdge && worldPoint.x < connection.x) ||
                (!useLeftEdge && worldPoint.x > connection.x))
            {
                connection = worldPoint;
                found = true;
            }
        }

        if (found)
        {
            return connection;
        }

        return new Vector2(
            useLeftEdge ? GetLeftEdge(segment) : GetRightEdge(segment),
            segment.position.y
        );
    }

    private Bounds? GetRenderedBounds(Transform segment)
    {
        SpriteRenderer[] renderers = segment.GetComponentsInChildren<SpriteRenderer>();
        if (renderers.Length == 0)
        {
            return null;
        }

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        return bounds;
    }

    private float GetRecycleBoundary()
    {
        if (mainCamera == null || !mainCamera.orthographic)
        {
            return player.position.x - recycleBehindDistance;
        }

        float halfCameraWidth = mainCamera.orthographicSize * mainCamera.aspect;
        return mainCamera.transform.position.x - halfCameraWidth - recycleBehindDistance;
    }
}
