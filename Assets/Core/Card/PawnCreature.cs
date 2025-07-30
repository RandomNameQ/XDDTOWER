using Core.Card;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(GridPlacement))]
[RequireComponent(typeof(Drag3dObject))]
public class PawnCreature : MonoBehaviour
{
    public CardData cardData;
    public Image avatarFace;

    [Header("Размер фигуры (в клетках)")]
    [SerializeField] private int sizeX = 1;
    [SerializeField] private int sizeZ = 1;

    public Enums.Team team;
    // как определить союзника или врага

    public GameObject projectile;
    public GameObject target;


    public enum PawnState
    {
        Idle,
        Ready,
        Dead
    }

    public PawnState pawnState = PawnState.Idle;

    private void Start()
    {
        if (InputManager.Instance != null)
        {
            InputManager.Instance.InputActions.Player.Mouse2.performed += InteractCard;
        }
        else
            Debug.Log("InputManager.Instance.InputActions.Player.Attack.performed += SellBody;");
    }


    private void Awake()
    {
        Init();

    }
    public void Init()
    {
        if (cardData == null)
        {
            Debug.LogWarning("CardData is not set");
            return;
        }
        InitCreature();
        InitCardData();

        sizeX = cardData.sizeX;
        sizeZ = cardData.sizeZ;
        var placement = GetComponent<GridPlacement>();
        placement.SetSize(sizeX, sizeZ);

    }
    public void InitCreature()
    {
        ResetCreature();

    }

    public void InitCardData()
    {
    }

    public void OnEnable()
    {
        BattleEvent.OnStartFight += () => pawnState = PawnState.Ready;
    }
    private void OnDisable()
    {
        BattleEvent.OnStartFight -= () => pawnState = PawnState.Ready;
        if (InputManager.Instance != null)
        {
            InputManager.Instance.InputActions.Player.Mouse2.performed -= InteractCard;
        }
        else
            Debug.Log("InputManager.Instance.InputActions.Player.Attack.performed -= SellBody;");
    }





    private void Update()
    {
        UpdateCooldown();
    }

    private void UpdateCooldown()
    {
        if (pawnState == PawnState.Dead || pawnState == PawnState.Idle) return;

        if (cardData.cardRang[0].offensiveData.remainingCooldown > 0)
        {
            cardData.cardRang[0].offensiveData.remainingCooldown -= Time.deltaTime;

            if (0 >= cardData.cardRang[0].offensiveData.remainingCooldown)
            {
                Attack();
                cardData.cardRang[0].offensiveData.remainingCooldown = cardData.cardRang[0].offensiveData.cooldown;

            }
        }
    }



    public void Attack()
    {
        var projectileInstance = Instantiate(projectile, transform.position, Quaternion.identity);
        projectileInstance.GetComponent<ProjectileBase>().target = target;
    }



    public void InteractCard(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            // Получаем луч от камеры в направлении курсора мыши
            Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
            RaycastHit hit;

            // Проверяем, попал ли луч в коллайдер
            if (Physics.Raycast(ray, out hit))
            {
                // Проверяем, что луч попал в текущий объект
                if (hit.collider.gameObject == gameObject)
                {
                    var interactCardUI = FindAnyObjectByType<InteractCard_UI>(FindObjectsInactive.Include);
                    if (interactCardUI != null)
                    {
                        interactCardUI.gameObject.SetActive(true);
                        interactCardUI.Init(this, Mouse.current.position.ReadValue());
                    }
                    else
                    {
                        Debug.LogError("InteractCard_UI not found in the scene.");
                    }
                }
            }
        }
    }

    public void RemoveCard()
    {
        GetComponent<GridPlacement>().RemoveCard();
        Destroy(gameObject);
    }


    public void ResetCreature()
    {
        pawnState = PawnState.Idle;
    }

    [Button]
    public void InstallAvatar()
    {
        GetComponentInChildren<PawnFace_Comp>().GetComponent<Image>().sprite = cardData.image;

    }
}
