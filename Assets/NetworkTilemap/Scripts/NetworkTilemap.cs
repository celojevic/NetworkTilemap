// only available in 2022.1+ because the Tilemap.tilemapTileChanged callback now exists at runtime
#if UNITY_2022_1_OR_NEWER && FISHNET

using FishNet.Managing.Logging;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using System.Collections;
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


        [Tooltip("True to automatically cache the tiles in the sibling tilemap to the _tiles list.")]
        [SerializeField] 
        private bool _cacheCurrentTilesInMap = true;

        [Tooltip("True to clear all map tiles when server starts.")]
        [SerializeField] 
        private bool _clearTilemapOnServerStart = true;

        [Tooltip("List of tiles shared between server and client.")]
        [SerializeField] 
        private List<TileBase> _tiles = new List<TileBase>();
        public List<TileBase> Tiles => _tiles;

        [Tooltip("Sibling tilemap component to sync over the network.")]
        [SerializeField] 
        private Tilemap _tilemap = null;
        public Tilemap Tilemap => _tilemap;

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

            if (_tiles.Count == 0)
            {
                if (_logLevel >= LoggingType.Warning)
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

            // test
            //StartCoroutine(SetTileTest());
        }

        IEnumerator SetTileTest()
        {
            while (true)
            {
                if (_tiles.Count > 0)
                {
                    int x = Random.Range(0, 10);
                    int y = Random.Range(0, 10);
                    var rand = _tiles[Random.Range(0, _tiles.Count)];

                    _tilemap.SetTile(new Vector3Int(x, y, 0), rand);
                }

                yield return new WaitForSeconds(2f);
            }
        }

        public override void OnStopServer()
        {
            base.OnStopServer();

            StopAllCoroutines();
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

            foreach (var item in tiles)
            {
                if (!_syncTiles.ContainsKey(item.position))
                {
                    _syncTiles.Add(item.position, new NetworkTileData { Position = item.position, TileName = item.tile.name });
                    if (_logLevel >= LoggingType.Common)
                        Debug.Log($"Added tile {item.tile.name} at {item.position}");
                }
                else
                {
                    _syncTiles[item.position] = new NetworkTileData { Position = item.position, TileName = item.tile.name };
                    if (_logLevel >= LoggingType.Common)
                        Debug.Log($"Set tile {item.tile.name} at {item.position}");
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
