#if UNITY_2022_1_OR_NEWER && FISHNET

using FishNet.Object;
using FishNet.Object.Synchronizing;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace gooby.NetworkTilemaps
{

    [RequireComponent(typeof(Grid))]
    public class NetworkGrid : NetworkBehaviour
    {

        [Tooltip("Child NetworkTilemaps.")]
        [SerializeField] private NetworkTilemap[] _networkTilemaps = null;

        [SerializeField] private Grid _grid = null;


        #region Grid Values

        [Tooltip("The size of each cell in the Grid.")]
        [SyncVar(OnChange = nameof(OnCellSizeChanged))]
        public Vector3 CellSize;
        private void OnCellSizeChanged(Vector3 prev, Vector3 next, bool asServer) => _grid.cellSize = next;

        [Tooltip("The size of the gap between each cell in the Grid.")]
        [SyncVar(OnChange = nameof(OnCellGapChanged))]
        public Vector3 CellGap;
        private void OnCellGapChanged(Vector3 prev, Vector3 next, bool asServer) => _grid.cellGap = next;

        [Tooltip("The layout of the cells in the Grid.")]
        [SyncVar(OnChange = nameof(OnCellLayoutChanged))]
        public GridLayout.CellLayout CellLayout;
        private void OnCellLayoutChanged(GridLayout.CellLayout prev, GridLayout.CellLayout next, bool asServer) => _grid.cellLayout = next;

        [Tooltip("The cell swizzle for the Grid.")]
        [SyncVar(OnChange = nameof(OnCellSwizzleChanged))]
        public GridLayout.CellSwizzle CellSwizzle;
        private void OnCellSwizzleChanged(GridLayout.CellSwizzle prev, GridLayout.CellSwizzle next, bool asServer) => _grid.cellSwizzle = next;

        #endregion


        public override void OnStartServer()
        {
            base.OnStartServer();

            // set inspector defaults
            CellSize = _grid.cellSize;
            CellGap = _grid.cellGap;
            CellLayout = _grid.cellLayout;
            CellSwizzle = _grid.cellSwizzle;
        }

        protected override void OnValidate()
        {
            base.OnValidate();

            if (_grid == null)
                _grid = GetComponent<Grid>();

            _networkTilemaps = GetComponentsInChildren<NetworkTilemap>();
        }

    }

}

#endif
