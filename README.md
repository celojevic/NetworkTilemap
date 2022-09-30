# NetworkTilemap
Networked tilemap synchronizer for FishNet.
 
# Unity version (IMPORTANT!)
2022.1 or newer
- Unity has made the Tilemap.tilemapTileChanged event available during runtime from this version forward.
 
# Setup
- Create a Tilemap like usual
-- (Optional) Attach a NetworkGrid to the parent Grid object
- Attach the NetworkTilemap component to the newly created Tilemap object
- Add tiles to its tiles list which you want to be synced over the network.

# Usage
Make any edits directly to the Tilemap component on the *SERVER* side. The server will catch the change events, update the serializable tile data in a SyncDictionary, and the SyncDictionary callback update it on clients.