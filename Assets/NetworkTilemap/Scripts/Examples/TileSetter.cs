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
        private NetworkTilemap _tilemap = null;

        private void Awake()
        {
            _tilemap = FindObjectOfType<NetworkTilemap>();
        }

        private void Update()
        {
            if (IsOwner)
            {
                if (Input.GetMouseButtonDown(0))
                {
                    CmdSetTile(Vector3Int.FloorToInt(Camera.main.ScreenToWorldPoint(Input.mousePosition)));
                }
                else if (Input.GetKeyDown(KeyCode.Escape))
                {
                    CmdClearAllTiles();
                }                
            }
        }

        [ServerRpc]
        void CmdClearAllTiles()
        {
            _tilemap.Tilemap.ClearAllTiles();
        }

        [ServerRpc]
        void CmdSetTile(Vector3Int tilePos)
        {
            _tilemap.Tilemap.SetTile(tilePos, _tile);
        }

    }

}
