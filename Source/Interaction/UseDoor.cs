using System.Collections;
using System.Collections.Generic;
using UnityEditorInternal.Profiling.Memory.Experimental;
using UnityEngine;

public class UseDoor : MonoBehaviour
{
    private Inventory inventory;

    [SerializeField] private int DialogueIndex;
    private int index = 1;
    private int dialoguePoint = 0;

    [SerializeField] private int changeCount = 0;
    [SerializeField] private int[] changePoint;
    [SerializeField] private GameObject mateDoor;
    [SerializeField] private bool needPassword;
    [SerializeField] private string password = "1234";
    [SerializeField] private bool needKey;
    [SerializeField] private GameObject key;
    [SerializeField] private bool needChoice;
    [SerializeField] private bool open;

    CameraFollow cameraFollow;
    [SerializeField] GameObject nextRoom;

    private Item keyItem;
    private GameObject player;
    private ChoiceManager choiceManager;
    [SerializeField] KeypadManager keypadManager;

    public string Password => password;

    public AudioClip openDoorSound; // 문 열리는 소리
    private AudioSource audioSource;

    public Material outlineMaterial;
    private bool isOutlineEnabled = false;
    private MeshRenderer[] meshRenderers;
    private Material[][] originalMaterials;
    public float detectionRange = 5f;
    private Transform playerTransform;

    private void Start()
    {
        meshRenderers = GetComponentsInChildren<MeshRenderer>();
        originalMaterials = new Material[meshRenderers.Length][];

        for (int i = 0; i < meshRenderers.Length; i++)
        {
            // 메터리얼이 2개 이상인 경우에만 처리
            if (meshRenderers[i].materials.Length > 1)
            {
                originalMaterials[i] = meshRenderers[i].materials;
            }
        }

        player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
        }
        choiceManager = FindObjectOfType<ChoiceManager>();
        player = PlayerManager.Instance.transform.GetChild(0).gameObject;

        audioSource = GetComponent<AudioSource>();

        cameraFollow = FindObjectOfType<CameraFollow>();

        if (needKey)
        {
            inventory = player.GetComponent<Inventory>();
            keyItem = key.GetComponent<Item>();
        }
    }
    void Update()
    {
        if (playerTransform != null)
        {
            float distanceToPlayer = Mathf.Abs(playerTransform.position.x - transform.position.x);
            if (distanceToPlayer <= detectionRange)
            {
                SetOutline(true);
            }
            else
            {
                SetOutline(false);
            }
        }
    }
    public void SetOutline(bool enable)
    {
        if (isOutlineEnabled == enable) return;

        for (int i = 0; i < meshRenderers.Length; i++)
        {
            Material[] materials = meshRenderers[i].materials;

            // 메터리얼이 2개 이상인 경우에만 처리
            if (materials.Length > 1)
            {
                if (enable)
                {
                    materials[1] = outlineMaterial;
                }
                else
                {
                    materials[1] = originalMaterials[i][1];
                }

                meshRenderers[i].materials = materials;
            }
        }

        isOutlineEnabled = enable;
    }
    public void Interaction()
    {
        if (open)
        {
            if (openDoorSound != null)
            {
                audioSource.PlayOneShot(openDoorSound);
            }
            player.transform.position = mateDoor.transform.position;
            cameraFollow.SetMapSize(nextRoom);
        }
        else if (needKey && inventory.HasItem(keyItem))
        {
            Debug.Log(keyItem.itemName + "를 사용하였습니다");
            OpenDoor();
        }
        else if (needPassword)
        {
            keypadManager = FindObjectOfType<KeypadManager>();
            keypadManager.ShowKeypad(this);
        }
        else
        {
            DialogueManager.Instance.StartDialogue(DialogueIndex, index, gameObject);
            if (changePoint.Length > index - 1)
            {
                ChangeDialogue();
            }
        }
    }

    public void OpenDoor()
    {
        open = true;
    }

    private void ChangeDialogue()
    {
        dialoguePoint++;
        if (changeCount < changePoint.Length && changePoint[changeCount] <= dialoguePoint)
        {
            index++;
            changeCount++;
        }
    }
}
