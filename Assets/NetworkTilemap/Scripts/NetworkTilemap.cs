// only available in 2022.1+ because the Tilemap.tilemapTileChanged callback now exists at runtime
#if UNITY_2022_1_OR_NEWER && FISHNET

using FishNet.Managing.Logging;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace gooby.NetworkTilemaps
{

    [RequireComponent(typeof(Tilemap))]
    public class NetworkTilemap : NetworkBehaviour
    {

        [System.Serializable]
        public struct NetworkTileData
        {
            public Vector3Int Position;
            public string TileName;

            public override string ToString()
            {
                return $"{TileName} {Position}";
            }
        }

        [System.Serializable]
        public struct NetworkTilemapData
        {
            public string TilemapName;
            public List<Vector3Int> Position;

            public NetworkTilemapData(string tilemapName)
            {
                TilemapName = tilemapName;
                Position = new List<Vector3Int>();
            }

            public override string ToString()
            {
                return $"{TilemapName} {Position}";
            }
        }


        [Header("Network Tilemap Settings")]
        [Tooltip("Sibling tilemap component to sync over the network.")]
        [SerializeField]
        private Tilemap _tilemap = null;
        public Tilemap Tilemap => _tilemap;

        [Tooltip("True to automatically cache the tiles in the sibling tilemap to the _tiles list.")]
        [SerializeField] 
        private bool _cacheCurrentTilesInMap = true;

        [Tooltip("True to clear all map tiles when server starts. " +
            "Note that if _cacheCurrentTilesInMap is true, no tiles will be cached.")]
        [SerializeField] 
        private bool _clearTilemapOnServerStart = true;

        [Tooltip("List of tiles shared between server and client.")]
        [SerializeField] 
        private List<TileBase> _tiles = new List<TileBase>();
        public List<TileBase> Tiles => _tiles;


        [Header("Tilemap Settings")]
        [Tooltip("The frame rate for all Tile animations in the Tilemap.")]
        [SyncVar(OnChange = nameof(OnAnimationFrameRateChanged))]
        public float AnimationFrameRate = 1f;
        private void OnAnimationFrameRateChanged(float prev, float next, bool asServer) => _tilemap.animationFrameRate = next;

        [Tooltip("The color of the Tilemap layer.")]
        [SyncVar(OnChange = nameof(OnColorChanged))]
        public Color Color = Color.white;
        private void OnColorChanged(Color prev, Color next, bool asServer) => _tilemap.color = next;

        [Tooltip("Gets the anchor point of Tiles in the Tilemap.")]
        [SyncVar(OnChange = nameof(OnTileAnchorChanged))]
        public Vector3 TileAnchor = new Vector3(0.5f, 0.5f, 0f);
        private void OnTileAnchorChanged(Vector3 prev, Vector3 next, bool asServer) => _tilemap.tileAnchor = next;

        [Tooltip("Orientation of the Tiles in the Tilemap.")]
        [SyncVar(OnChange = nameof(OnOrientationChanged))]
        public Tilemap.Orientation Orientation = Tilemap.Orientation.XY;
        private void OnOrientationChanged(Tilemap.Orientation prev, Tilemap.Orientation next, bool asServer) => _tilemap.orientation = next;


        [Header("Debug")]
        [Tooltip("Set debug log level.")]
        [SerializeField]
        private LoggingType _logLevel = LoggingType.Common;

        /// <summary>
        /// All tile data currently sync'd over the network.
        /// </summary>
        [SyncObject]
        private readonly SyncDictionary<Vector3Int, NetworkTileData> _syncTiles = new SyncDictionary<Vector3Int, NetworkTileData>();


        private void Awake()
        {
            _tilemap = GetComponent<Tilemap>();

            if (_tiles.Count == 0 && _logLevel >= LoggingType.Warning)
            {
                Debug.LogWarning("No tiles set to sync on " + name);
            }
        }

        protected override void OnValidate()
        {
            base.OnValidate();

            if (_tilemap == null)
                _tilemap = GetComponent<Tilemap>();

            if (_cacheCurrentTilesInMap)
            {
                _tilemap.CompressBounds();
                TileBase[] tiles = _tilemap.GetTilesBlock(_tilemap.cellBounds);
                if (tiles.Length > 0)
                {
                    foreach (TileBase tile in tiles)
                    {
                        if (tile == null) continue;

                        if (!_tiles.Contains(tile))
                            _tiles.Add(tile);
                    }
                }
            }
        }

        public override void OnStartServer()
        {
            base.OnStartServer();

            if (_clearTilemapOnServerStart)
                _tilemap.ClearAllTiles();

            Tilemap.tilemapTileChanged += Tilemap_tilemapTileChanged;
        }

        public override void OnStopServer()
        {
            base.OnStopServer();

            Tilemap.tilemapTileChanged -= Tilemap_tilemapTileChanged;
        }

        private void Tilemap_tilemapTileChanged(Tilemap tilemap, Tilemap.SyncTile[] tiles)
        {
            // only sync tiles changed on server
            if (!IsServer) return;

            // we only care about the tilemap on this object
            if (_tilemap != tilemap) return;

            if (_logLevel >= LoggingType.Common)
                Debug.Log($"Tilemap: {tilemap.name}. Tile count: {tiles.Length}.");

            foreach (Tilemap.SyncTile item in tiles)
            {
                // removing tile
                if (item.tile == null && _syncTiles.ContainsKey(item.position))
                {
                    _syncTiles.Remove(item.position);
                    if (_logLevel >= LoggingType.Common)
                        Debug.Log($"Removed tile at {item.position}");
                }
                // adding tile
                else if (!_syncTiles.ContainsKey(item.position))
                {
                    _syncTiles.Add(item.position, new NetworkTileData { Position = item.position, TileName = item.tile?.name });
                    if (_logLevel >= LoggingType.Common)
                        Debug.Log($"Added tile {item.tile?.name} at {item.position}");
                }
                // updating existing tile
                else
                {
                    _syncTiles[item.position] = new NetworkTileData { Position = item.position, TileName = item.tile?.name };
                    if (_logLevel >= LoggingType.Common)
                        Debug.Log($"Set tile {item.tile?.name} at {item.position}");
                }
            }
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            _syncTiles.OnChange += SyncTiles_OnChange;
        }

        public override void OnStopClient()
        {
            base.OnStopClient();
            _syncTiles.OnChange -= SyncTiles_OnChange;
        }

        private void SyncTiles_OnChange(SyncDictionaryOperation op, Vector3Int key, NetworkTileData value, bool asServer)
        {
            if (_logLevel >= LoggingType.Common)
                Debug.Log($"Op: {op}. Key: {key}. Val: {value}. As server? {asServer}.");

            if (asServer || IsServer) return;

            switch (op)
            {
                case SyncDictionaryOperation.Clear:
                    _tilemap.ClearAllTiles();
                    break;

                case SyncDictionaryOperation.Remove:
                    _tilemap.SetTile(key, null);
                    break;

                case SyncDictionaryOperation.Add:
                case SyncDictionaryOperation.Set:
                    _tilemap.SetTile(key, GetTileByName(value.TileName));
                    break;
            }
        }

        private TileBase GetTileByName(string name)
        {
            return _tiles.Find(x => x.name == name);
        }

    }
}

#endif
