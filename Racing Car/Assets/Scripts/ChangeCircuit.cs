using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Mime;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class ChangeCircuit : MonoBehaviour
{
    [SerializeField] private List<GameObject> circuits;

    [SerializeField] private Text actualLap;
    [SerializeField] private Text actualNumber;
    [SerializeField] private List<Text> bestLap;

    private List<float> bestLapTime;

    private int nCircuits;

    private int lap = 1;

    private int previousCircuit;
    private int active = 0;

    private int totalLaps;
    private float totaltime = 0;

    //private List<int> totalLaps; // Test time after X laps
    void Start()
    {
        bestLapTime = new List<float> { 100000000000, 100000000000, 100000000000 };
        //totalLaps = new List<int> { 0, 0, 0 };
        nCircuits = circuits.Count;

        active = 0;
        circuits[active].SetActive(true);
    }

    private void OnTriggerEnter(Collider other)
    {
        int rnd = Random.Range(0, nCircuits);

        circuits[active].SetActive(false);
        previousCircuit = active;
        active = rnd;


        circuits[active].SetActive(true);
    }

    public void AddLap()
    {
        lap++;
        totalLaps++;
        int tLaps = 0;

        actualNumber.text = "Lap " + lap;

        if(totalLaps == 1 ) Debug.Log("Time to finish one lap: " + totaltime);
    }

    private void Update()
    {
        totaltime += Time.deltaTime;
    }

    public void SetBestLap(float time)
    {
        if (time < bestLapTime[previousCircuit])
        {
            bestLapTime[previousCircuit] = time;
            int min = Mathf.FloorToInt(bestLapTime[previousCircuit] / 60);
            int seg = Mathf.FloorToInt(bestLapTime[previousCircuit] % 60);
            int mili = Mathf.FloorToInt((bestLapTime[previousCircuit] * 1000) % 1000);

            string textTime = string.Format("{0:00}:{1:00}.{2:000}", min, seg, mili);

            bestLap[previousCircuit].text = textTime;
        }
    }

    public void SetActualLapTime(float actualLapTime)
    {
        int min = Mathf.FloorToInt(actualLapTime / 60);
        int seg = Mathf.FloorToInt(actualLapTime % 60);
        int mili = Mathf.FloorToInt((actualLapTime * 1000) % 1000);

        string time = string.Format("{0:00}:{1:00}.{2:000}", min, seg, mili);

        actualLap.text = time;
    }

    public void Reset()
    {
        circuits[active].SetActive(false);

        active = 0;
        previousCircuit = 0;
        circuits[active].SetActive(true);

        lap = 1;

        actualNumber.text = "Lap " + lap;
    }
}
