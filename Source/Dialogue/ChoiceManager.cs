using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Newtonsoft.Json;
using System.IO;

public class ChoiceManager : MonoBehaviour
{
    [System.Serializable]
    public class Choice
    {
        public string text;
        public string result;
    }

    [System.Serializable]
    public class ChoiceData
    {
        public Dictionary<string, List<Choice>> choices;
    }

    public GameObject choicePanel; // 선택지 패널 UI
    public GameObject choiceButtonPrefab; // 선택지 버튼 프리팹
    public Transform choiceButtonContainer; // 선택지 버튼이 담길 컨테이너

    private List<Button> choiceButtons = new List<Button>();
    private ChoiceData choiceData;
    private GameObject currentTarget; // 현재 상호작용 중인 오브젝트

    private TimeManager timeManager;

    void Start()
    {
        timeManager = FindObjectOfType<TimeManager>();
        choicePanel = transform.GetChild(0).gameObject;
        choicePanel.SetActive(false);
        LoadChoiceData();
    }

    void LoadChoiceData()
    {
        string jsonString = File.ReadAllText(Path.Combine(Application.streamingAssetsPath, "choices.json"));
        choiceData = JsonConvert.DeserializeObject<ChoiceData>(jsonString);
    }

    public void ShowChoices(string key, GameObject target)
    {
        if (choiceData.choices.TryGetValue(key, out List<Choice> choices))
        {
            timeManager.TogglePauseTime(true);
            currentTarget = target;
            choicePanel.SetActive(true);
            ClearChoices();

            for (int i = 0; i < choices.Count; i++)
            {
                var choice = choices[i];
                var choiceButton = Instantiate(choiceButtonPrefab, choiceButtonContainer).GetComponent<Button>();
                choiceButton.GetComponentInChildren<TextMeshProUGUI>().text = choice.text;
                int choiceIndex = i;

                choiceButton.onClick.AddListener(() =>
                {
                    HandleChoiceSelection(key, choiceIndex);
                    choicePanel.SetActive(false);
                });

                choiceButtons.Add(choiceButton);
            }
        }
        else
        {
            Debug.LogError($"No choices found for key: {key}");
        }
    }

    private void HandleChoiceSelection(string key, int choiceIndex)
    {
        timeManager.TogglePauseTime(false);
        var choice = choiceData.choices[key][choiceIndex];
        ICommand command = CommandFactory.CreateCommand(choice.result, currentTarget);
        command?.Execute();
    }

    private void ClearChoices()
    {
        foreach (var button in choiceButtons)
        {
            Destroy(button.gameObject);
        }
        choiceButtons.Clear();
    }
}

public interface ICommand
{
    void Execute();
}

public class UnlockDoorCommand : ICommand
{
    private GameObject target;

    public UnlockDoorCommand(GameObject target)
    {
        this.target = target;
    }

    public void Execute()
    {
        UseDoor useDoor = target.GetComponent<UseDoor>();
        if (useDoor != null)
        {
            useDoor.OpenDoor();
            Debug.Log("The door has been unlocked!");
        }
        else
        {
            Debug.LogError("The target does not have a UseDoor component.");
        }
    }
}

public class FindAnotherPathCommand : ICommand
{
    public void Execute()
    {
        Debug.Log("Finding another path...");
        // 실제 다른 길 찾기 로직 구현
    }
}

public static class CommandFactory
{
    public static ICommand CreateCommand(string result, GameObject target)
    {
        switch (result)
        {
            case "UnlockDoor":
                return new UnlockDoorCommand(target);
            case "FindAnotherPath":
                return new FindAnotherPathCommand();
            // 다른 명령어 추가
            default:
                Debug.LogError($"Unknown result: {result}");
                return null;
        }
    }
}
