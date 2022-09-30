using FishNet.Object;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace gooby.NetworkTilemaps.Examples
{

    public class TileSetter : NetworkBehaviour
    {

        [Tooltip("Tile to set when clicking.")]
        [SerializeField] 
        private TileBase _tile = null;

        [Tooltip("NetworkTilemap to set the tile on.")]
        [SerializeField]
        private NetworkTilemap _netTilemap = null;

        private void Awake()
        {
            _netTilemap = FindObjectOfType<NetworkTilemap>(true);
        }

        private void Update()
        {
            if (IsOwner)
            {
                if (Input.GetMouseButtonDown(0))
                {
                    Vector3Int tilePos = Vector3Int.FloorToInt(Camera.main.ScreenToWorldPoint(Input.mousePosition));
                    tilePos.z = 0;
                    CmdSetTile(tilePos);
                }
                else if (Input.GetMouseButtonDown(1))
                {
                    Vector3Int tilePos = Vector3Int.FloorToInt(Camera.main.ScreenToWorldPoint(Input.mousePosition));
                    tilePos.z = 0;
                    if (_netTilemap.Tilemap.HasTile(tilePos))
                        CmdRemoveTile(tilePos);
                }
                else if (Input.GetKeyDown(KeyCode.Escape))
                {
                    if (_netTilemap.Tilemap.GetUsedTilesCount() > 0)
                        CmdClearAllTiles();
                }                
            }
        }

        [ServerRpc]
        void CmdClearAllTiles()
        {
            _netTilemap.Tilemap.ClearAllTiles();
        }

        [ServerRpc]
        void CmdSetTile(Vector3Int tilePos)
        {
            _netTilemap.Tilemap.SetTile(tilePos, _tile);
        }

        [ServerRpc]
        void CmdRemoveTile(Vector3Int tilePos)
        {
            _netTilemap.Tilemap.SetTile(tilePos, null);
        }

    }

}
