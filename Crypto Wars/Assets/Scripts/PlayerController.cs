using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class PlayerController : MonoBehaviour
{
    [SerializeField]
    private GameObject attackButton;
    [SerializeField]
    private GameObject destroyButton;
    [SerializeField]
    private GameObject buildButton;
    [SerializeField]
    private GameObject progressBar;
    [SerializeField]
    private GameObject cancelButton;
    [SerializeField]
    private Stash stash;
    public static List<Player> players { get; set; }
    private static int CurrentPlayerIndex;

    // Tracks the player who is currently playing
    public static Player CurrentPlayer { get; set; }
    public static bool Switching = false;
    // Store the most recently selected tile
    private static Tile selectedTile;
    private static GameObject selectedGameObject;
    
    private bool notAdj = false;
    private float offSet = 0.04f;

    public void SetAdj(bool boo)
    {
        notAdj = !boo;
    }

    public Stash GetStash()
    {
        return stash;
    }

    // Get number of players in game
    public int GetNumberOfPlayers(){
        return players.Count;
    }

    // Start is called before the first frame update
    void Awake()
    {
        players = new List<Player>
        {
            new Player("One", Resources.Load<Material>("Materials/PlayerTileColor")),
            new Player("Two", Resources.Load<Material>("Materials/EnemyTileColor"))
        };

        
        CurrentPlayer = players[0];
        CurrentPlayerIndex = 0;
        CurrentPlayer.SetPhase(Player.Phase.Defense);
    }

    // Update is called once per frame
    void Update()
    {
        if(Switching)
            DisableButtonCanvas();
        // Detects input of the player inside the camera
        // Right now it just does some basic stuff
        // Will have more functionality in future
        if (Input.GetMouseButton(0))
        {
            RaycastHit hit;
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out hit, 100f)) {
                if (hit.transform != null) {
                    GameObject obj = hit.transform.gameObject;
                    if (obj.GetComponent<Tile>() != null) {
                        Tile tile = obj.GetComponent<Tile>();
                        TileInteractions(tile);
                    }
                    else if(obj != null) {
                        BuildingInteractions(obj);
                    }
                }
            }
        }
        // Temp player switching until TurnMaster additions can be made
        /*
        if (Input.GetKeyDown(KeyCode.K)) {
            NextPlayer();
            Debug.Log("Next: Player Index is now: " + CurrentPlayerIndex);
        }
        */

        // DEBUG: allows current player to take tile regardless of who owns it
        /*
        if (Input.GetMouseButton(1))
        {
            RaycastHit hit;
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out hit, 100f)) {
                if (hit.transform != null) {
                    Tile tile = hit.transform.GetComponent<Tile>();

                    if (tile != null) {
                        tile.SetPlayer(CurrentPlayerIndex);
                        tile.SetMaterial(players[CurrentPlayerIndex].GetColor());
                    }
                }
            }
        }
        */
    }

    public void TileInteractions(Tile tile) {
        if (tile != null)
        {
            // Grabs a new tile to check and makes sure it's not being checked multiple times
            if ((selectedTile == null || !selectedTile.GetTilePosition().Equals(tile.GetTilePosition())) && !stash.gameObject.activeSelf)
            {
                SetSelectedTile(tile); // Sets the controller's tile that was last clicked
                attackButton.GetComponent<AttackButtonScript>().ResetClicks();
                if (Tile.IsAdjacent(CurrentPlayer, tile))
                {
                    notAdj = false;
                }
                else
                {
                    notAdj = true;
                }
                // If the tile clicked on is not controlled by the current player
                if (tile.GetPlayer() != CurrentPlayerIndex && CurrentPlayer.GetCurrentPhase() == Player.Phase.Attack)
                {
                    SetupAttackButton(tile);
                }
                // If the tile clicked on and the player owns it to build
                if (tile.GetPlayer() == CurrentPlayerIndex && CurrentPlayer.GetCurrentPhase() == Player.Phase.Build)
                {
                    SetupBuildButton(tile);
                }
                // If the tile clicked on and the player wants to defend it from an attack
                // (If an attack exists)
                if (tile.GetPlayer() == CurrentPlayerIndex && CurrentPlayer.GetCurrentPhase() == Player.Phase.Defense)
                {
                    // Grabs the tile to check if it can be defended
                    if (CreateDefenseSystem.IsDefendable(tile.GetTilePosition()))
                    {
                        stash.Activate(true);
                    }
                    else
                    {
                        stash.Activate(false);
                    }

                }
            }
        }
    }

    public void BuildingInteractions(GameObject obj) {
        if (obj.tag.Equals("IsBuilding"))
        {
            if (selectedGameObject == null || !selectedGameObject.transform.position.Equals(obj.transform.position))
            {
                selectedGameObject = obj;
                List<Tile.TileReference> tiles = CurrentPlayer.GetTiles();

                float percentage = 0;
                foreach (Tile.TileReference buildingTile in tiles)
                {
                    if (!buildingTile.currBuilding.Equals("Nothing") && buildingTile.currBuilding.GetOwner() != null && buildingTile.currBuilding.GetOwner().GetName().Equals(CurrentPlayer.GetName()))
                    {
                        Vector2 buildingPos = new Vector2(Mathf.FloorToInt(buildingTile.currBuilding.GetPosition().x), Mathf.FloorToInt(buildingTile.currBuilding.GetPosition().y));
                        Vector2 tilePos = new Vector2(Mathf.FloorToInt(selectedGameObject.transform.position.x), Mathf.FloorToInt(selectedGameObject.transform.position.z));

                        if (buildingPos.Equals(tilePos))
                        {
                            percentage = buildingTile.currBuilding.GetPercentageFilled();
                            MoveProgress(buildingTile);
                        }
                    }
                }

                progressBar.transform.Find("Slider").GetComponent<Slider>().value = percentage;
                Debug.Log("nice");
            }

        }
    }

    // Moves to the next player in line
    public static void NextPlayer() {
        Switching = true;
        if (players.Count > (CurrentPlayerIndex + 1))
        {
            CurrentPlayerIndex++; 
        }
        else {
            CurrentPlayerIndex = 0;
        }
        CurrentPlayer = players[CurrentPlayerIndex];
    }

    // Grabs a player based on position in players array
    public static void NextPlayer(int index)
    {
        Switching = true;
        if (players.Count >= (index + 1))
        {
            CurrentPlayerIndex = index;
            CurrentPlayer = players[index];
        }
    }
    public static int GetCurrentPlayerIndex()
    {
        return CurrentPlayerIndex;
    }

    public static List<Player> GetPlayerList()
    {
        return players;
    }
    
    public static Player GetCurrentPlayer()
    {
        return players[GetCurrentPlayerIndex()];
    }

    public static Tile GetSelectedTile()
    {
        return selectedTile;
    }

    public static void SetSelectedTile(Tile tile)
    {
        selectedTile = tile;
    }
    public void EnableCancel()
    {
        cancelButton.SetActive(true);
    }

    public GameObject GetCancelButton() {
        return cancelButton;
    }

    public void MoveProgress(Tile.TileReference tile)
    {
        attackButton.SetActive(false);
        buildButton.SetActive(false);
        destroyButton.SetActive(false);
        progressBar.SetActive(true);

        progressBar.transform.position = new Vector3(tile.tilePosition.x + offSet, 2.5f, tile.tilePosition.y + .3f);
        progressBar.transform.localScale = new Vector3(0.005f, 0.005f, 0.005f);
        progressBar.transform.eulerAngles = new Vector3(90, 0, 0);
    }

    public GameObject GetProgress() { 
        return progressBar;
    }

    public void MoveBuild(Tile tile)
    {
        attackButton.SetActive(false);
        buildButton.SetActive(true);
        destroyButton.SetActive(false);
        progressBar.SetActive(false);
        buildButton.GetComponent<BuildButtonScript>().DeactivateMenu();

        buildButton.transform.position = new Vector3(tile.GetTilePosition().x + offSet, 2.5f, tile.GetTilePosition().y + .45f);
        buildButton.transform.localScale = new Vector3(0.005f, 0.005f, 0.005f);
        buildButton.transform.eulerAngles = new Vector3(90, 0, 0);
    }

    public GameObject GetBuild(){
        return buildButton;
    }
    public void MoveDestroy(Tile tile)
    {
        attackButton.SetActive(false);
        buildButton.SetActive(false);
        destroyButton.SetActive(true);
        progressBar.SetActive(false);

        destroyButton.transform.position = new Vector3(tile.GetTilePosition().x + offSet, 2.5f, tile.GetTilePosition().y + .45f);
        destroyButton.transform.localScale = new Vector3(0.005f, 0.005f, 0.005f);
        destroyButton.transform.eulerAngles = new Vector3(90, 0, 0);
    }
    public GameObject GetDestroy(){
        return destroyButton;
    }
    public void MoveAttack(Tile tile) {
        attackButton.SetActive(true);
        buildButton.SetActive(false);
        destroyButton.SetActive(false);
        progressBar.SetActive(false);

        attackButton.transform.position = new Vector3(tile.GetTilePosition().x + offSet, 2.5f, tile.GetTilePosition().y);
        attackButton.transform.localScale = new Vector3(0.055f, 0.055f, 0.055f);
        attackButton.transform.eulerAngles = new Vector3(90, 0, 0);
    }
    public GameObject GetAttack(){
        return attackButton;
    }

    public void DisableButtonCanvas() {
        selectedGameObject = null;
        selectedTile = null;
        attackButton.SetActive(false);
        buildButton.SetActive(false);
        destroyButton.SetActive(false);
        progressBar.SetActive(false);
        buildButton.GetComponent<BuildButtonScript>().DeactivateMenu();
    }
    public void SetupAttackButton(Tile tile) {
        if (!notAdj) {
            MoveAttack(tile);
            EnableCancel();
        }
    }
    public void SetupBuildButton(Tile tile) {
        if (tile.GetBuilding().GetName() == "Nothing")
        {
            MoveBuild(tile);
            EnableCancel();

            Button bldButton = buildButton.GetComponent<Button>();
   
            bldButton.onClick.AddListener(() => buildButton.GetComponent<BuildButtonScript>().DeactivateMain());
            Debug.Log(tile.GetBuilding().GetName());
            Debug.Log("Creating a Build Button");
        }
        else 
        {
            MoveDestroy(tile);
            EnableCancel();

            Button desButton = destroyButton.GetComponent<Button>();

            desButton.onClick.AddListener(() => buildButton.GetComponent<BuildButtonScript>().DeletePrefab(tile));
            Debug.Log(tile.GetBuilding().GetName());
            Debug.Log("Creating a Destroy Button");
        }
        
    }
}
