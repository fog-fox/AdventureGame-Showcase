using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.IO;
using Newtonsoft.Json;
using System;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

    [System.Serializable]
    public class DialogueData
    {
        public Dictionary<string, List<Dialogue>> dialogues;
    }

    [System.Serializable]
    public class Dialogue
    {
        public int id;
        public string speaker;
        public string[] lines;
        public string choice;
        public Illustration illustration;
    }

    [System.Serializable]
    public class Illustration
    {
        public string characterName;
        public int face;
        public int pos;
        public int endPos;
    }

    [SerializeField] private string dialogueFilePath = "dialogue.json"; // 대화 파일 경로
    private DialogueData dialogueData; // 대화 데이터

    public GameObject dialoguePanel; // 대화 패널 UI
    public TextMeshProUGUI speakerNameText; // 화자 이름 UI Text 컴포넌트
    public TextMeshProUGUI dialogueText; // 대화 내용 UI Text 컴포넌트
    public Image[] characterImages; // 캐릭터 이미지 UI Image 컴포넌트
    private Color hideColor = Color.white;

    private TimeManager timeManager;
    private PlayerController playerController;

    private int linesCount;
    private int currentDialogueIndex;
    private List<Dialogue> currentDialogues;
    private Dialogue currentDialogue;

    public ChoiceManager choiceManager;

    private GameObject targetObjects;
    private Coroutine typingCoroutine;

    public float typingSpeed = 0.5f;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 씬 전환 시에도 유지
        }
        else
        {
            Destroy(gameObject);
        }
    }
    void Start()
    {
        playerController = GameObject.Find("PlayerManager").transform.GetChild(0).gameObject.GetComponent<PlayerController>();
        hideColor.a = 0;
        timeManager = FindObjectOfType<TimeManager>();
        choiceManager = FindObjectOfType<ChoiceManager>();
        speakerNameText = dialoguePanel.GetComponentInChildren<TextMeshProUGUI>();
        dialogueText = dialoguePanel.GetComponentsInChildren<TextMeshProUGUI>()[1];
        characterImages = dialoguePanel.transform.GetChild(1).GetComponentsInChildren<Image>();
        ResetCharacterImages();
        dialoguePanel.SetActive(false);

        LoadDialogueData();
    }

    void Update()
    {
        if (dialoguePanel != null)
        {
            if (Input.GetKeyDown(KeyCode.LeftControl) && dialoguePanel.activeSelf)
            {
                if (typingCoroutine != null)
                {
                    StopCoroutine(typingCoroutine);
                    typingCoroutine = null;
                    dialogueText.text = currentDialogue.lines[linesCount]; // 대화 텍스트를 즉시 출력
                }
                else
                {
                    NextDialogue();
                }
            }
        }
    }

    void LoadDialogueData()
    {
        try
        {
            // JSON 파일 읽기
            string jsonString = File.ReadAllText(Path.Combine(Application.streamingAssetsPath, dialogueFilePath));
            Debug.Log($"Loaded JSON string: {jsonString}");

            // JSON 데이터 파싱
            dialogueData = JsonConvert.DeserializeObject<DialogueData>(jsonString);

            if (dialogueData == null || dialogueData.dialogues == null)
            {
                Debug.LogError("Failed to load dialogue data or dialogues are null.");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("Exception while loading dialogue data: " + ex.Message);
        }
    }

    // 대화 시작
    public void StartDialogue(int dialogueCategory, int dialogueSequence, GameObject targetObject)
    {
        if (targetObject == null)
        {
            Debug.LogError("Target object is null in StartDialogue");
            return;
        }
        string dialogueKey = $"{dialogueCategory}-{dialogueSequence}";
        targetObjects = targetObject;
        currentDialogueIndex = 0;
        linesCount = 0;
        playerController.CanInteraction();
        if (dialogueData.dialogues.TryGetValue(dialogueKey, out currentDialogues))
        {
            dialoguePanel.SetActive(true);
            DisplayCurrentDialogue();
            timeManager.TogglePauseTime(true);
        }
        else
        {
            Debug.LogError("Dialogue key or index not found in dialogues.");
        }
    }

    public void EndDialogue()
    {
        playerController.CanInteraction();
        ResetCharacterImages();
        dialoguePanel.SetActive(false);
        if (currentDialogue.choice != null)
        {
            choiceManager.ShowChoices(currentDialogue.choice, targetObjects);
        }
        else timeManager.TogglePauseTime(false);
    }

    private void NextDialogue()
    {
        if (typingCoroutine != null)
        {
            // 현재 코루틴이 동작 중이면 전체 텍스트를 출력하고 코루틴 종료
            StopCoroutine(typingCoroutine);
            dialogueText.text = currentDialogue.lines[linesCount];
            typingCoroutine = null;
            return;
        }

        if (currentDialogue.lines.Length - 1 > linesCount)
        {
            linesCount++;
            DisplayCurrentDialogue();
        }
        else
        {
            linesCount = 0;
            if (currentDialogue.illustration != null)
            {
                ImageProcessing(currentDialogue.illustration.endPos);
            }
            currentDialogueIndex++;

            if (currentDialogueIndex < currentDialogues.Count)
            {
                currentDialogue = currentDialogues[currentDialogueIndex];
                DisplayCurrentDialogue();
            }
            else
            {
                EndDialogue();
            }
        }
    }

    private void DisplayCurrentDialogue()
    {
        currentDialogue = currentDialogues[currentDialogueIndex];
        speakerNameText.text = currentDialogue.speaker;

        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
        }
        typingCoroutine = StartCoroutine(TypeSentence(currentDialogue.lines[linesCount]));

        if (currentDialogue.illustration != null)
        {
            string characterName = currentDialogue.illustration.characterName;
            int faceIndex = currentDialogue.illustration.face;
            int pos = currentDialogue.illustration.pos;

            Sprite[] characterSprites = Resources.LoadAll<Sprite>("CharacterIllustrations/" + characterName);

            if (characterSprites != null && characterSprites.Length > faceIndex)
            {
                Sprite characterSprite = characterSprites[faceIndex];
                characterImages[pos].sprite = characterSprite;
                characterImages[pos].color = Color.white;
            }
            else
            {
                Debug.LogError("Failed to load character illustrations for character: " + characterName);
            }
        }
    }

    private IEnumerator TypeSentence(string sentence)
    {
        dialogueText.text = ""; // 출력된 텍스트 초기화
        foreach (char letter in sentence.ToCharArray())
        {
            dialogueText.text += letter;
            yield return new WaitForSecondsRealtime(typingSpeed); // 글자 출력 간격
        }
        typingCoroutine = null; // 코루틴이 끝났음을 표시
    }

    void ResetCharacterImages()
    {
        foreach (Image image in characterImages)
        {
            image.sprite = null;
            image.color = hideColor;
        }
    }

    void ImageProcessing(int num)
    {
        if (currentDialogue.illustration != null)
        {
            int pos = currentDialogue.illustration.pos;
            switch (num)
            {
                case 0: // 일러스트 비활성화(회색)
                    characterImages[pos].color = Color.gray;
                    break;
                case 1: // 일러스트 제거(투명)
                    characterImages[pos].color = hideColor;
                    break;
            }
        }
    }
}
