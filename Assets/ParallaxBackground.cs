using UnityEngine;

public class ParallaxBackground : MonoBehaviour
{
    [Range(0f, 1f)]
    public float horizontalMultiplier = 0.03f;
    [Min(3)]
    public int tileCount = 3;

    private Transform cameraTransform;
    private Transform[] tiles;
    private float cameraStartX;
    private float tileWidth;
    private float leftMostX;

    void Start()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            enabled = false;
            return;
        }

        cameraTransform = mainCamera.transform;
        cameraStartX = cameraTransform.position.x;

        SpriteRenderer renderer = GetComponent<SpriteRenderer>();
        if (renderer == null)
        {
            enabled = false;
            return;
        }

        tileWidth = renderer.bounds.size.x;
        leftMostX = transform.position.x;
        tiles = new Transform[tileCount];
        tiles[0] = transform;

        for (int i = 1; i < tileCount; i++)
        {
            Transform clone = Instantiate(transform, transform.parent);
            clone.name = gameObject.name + "_Tile_" + i;
            clone.position = transform.position + Vector3.right * tileWidth * i;

            ParallaxBackground cloneParallax = clone.GetComponent<ParallaxBackground>();
            if (cloneParallax != null)
            {
                Destroy(cloneParallax);
            }

            tiles[i] = clone;
        }
    }

    void LateUpdate()
    {
        if (cameraTransform == null || tiles == null)
        {
            return;
        }

        float cameraDeltaX = cameraTransform.position.x - cameraStartX;
        float parallaxOffset = cameraDeltaX * horizontalMultiplier;
        float leftBoundary = cameraTransform.position.x - tileWidth;
        float firstTileRightEdge = leftMostX + parallaxOffset + tileWidth;

        if (firstTileRightEdge < leftBoundary)
        {
            float distanceBehind = leftBoundary - firstTileRightEdge;
            int tilesToAdvance = Mathf.FloorToInt(distanceBehind / tileWidth) + 1;
            leftMostX += tileWidth * tilesToAdvance;
        }

        for (int i = 0; i < tiles.Length; i++)
        {
            float tiledX = leftMostX + tileWidth * i + parallaxOffset;

            tiles[i].position = new Vector3(
                tiledX,
                tiles[i].position.y,
                tiles[i].position.z
            );
        }
    }
}
