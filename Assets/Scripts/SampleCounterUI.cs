using UnityEngine.UI;
using UnityEngine;
using UnityEngine.Events;


public class SampleCounterUI : MonoBehaviour
{
    public Text sampleCounterText;
    public uint sampleCount;
    public Text executedTimeText;
    public float executedTime;

    private void Awake()
    {
        executedTime = 0;
    }

    private void Update()
    {
        executedTime += Time.deltaTime;
        executedTimeText.text = "Executed Time: " + executedTime.ToString("F2");
    }
}
