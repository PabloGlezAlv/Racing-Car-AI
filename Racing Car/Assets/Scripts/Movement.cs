using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Purchasing;

[RequireComponent(typeof(NeuralNetwork))]
public class Movement : MonoBehaviour
{
    private GeneticManager geneticManager;
    [SerializeField] ChangeCircuit circuitManager;

    [Range(-1f, 1f)]
    public float acceleration, rotation;

    [SerializeField] private bool setTime = false;

    [SerializeField] float maxDistance = 20;

    [SerializeField] float speed = 20f;


    [SerializeField] float lifeTime = 0f;

    [SerializeField] int nVectors = 0;

    [Header("Fitness")]
    public float overallFitness;
    public float distanceMultipler = 1.4f;
    public float avgSpeedMultiplier = 0.2f;
    public float sensorMultiplier = 0.1f;
    public float cornerMultiplier = 2f;
    public float lapTimeMultiplier = 2f;

    [Header("Dead Cars")]
    [SerializeField] float maxlifeTime = 120.0f;
    [SerializeField] float fitnessPoints = 40.0f;

    private Vector3 startPosition, startRotation, lastPosition;
    private float distancetravelled, avgSpeed;

    private float rightSensor, rightMidSensor, leftMidSensor, frontSensor, leftSensor;

    private float lapPoints = 0; 

    private float cornerValue = 0;

    private Vector3 input;

    private NeuralNetwork network;

    private ChangeCircuit timerCanva;

    private float lapTime = 0f;

    private int numLaps = 0;

    private void Awake() {
        startPosition = transform.position;
        startRotation = transform.eulerAngles;
        network = GetComponent<NeuralNetwork>();

        GameObject finishLine = GameObject.FindGameObjectWithTag("finishLine");

        timerCanva = finishLine.GetComponent<ChangeCircuit>();

        geneticManager = GameObject.FindFirstObjectByType<GeneticManager>();
    }

    public void ResetWithNetwork (NeuralNetwork net)
    {
        network = net;
        Reset();
    }

    public void Reset() {

        lifeTime = 0f;
        distancetravelled = 0f;
        avgSpeed = 0f;
        lastPosition = startPosition;
        overallFitness = 0f;
        transform.position = startPosition;
        transform.eulerAngles = startRotation;
        cornerValue = 0;

        lapTime = 0;
        numLaps = 0;
        lapPoints = 0;
    }

    private void OnCollisionEnter (Collision collision) {
        if(collision.gameObject.CompareTag("wall")) // Destroy car
        {
            Death();
            circuitManager.Reset();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("corners")) // Calculate corner fitness of the car
        {
            Transform reference = other.gameObject.GetComponentInChildren<Transform>();

            //Calculate max distance
            float dist = Vector3.Distance(transform.position, reference.position);

            if (dist < 10)
            {
                cornerValue += (10 - dist);
            }
        }
        else if (other.gameObject.CompareTag("finishLine") && numLaps > 0)// Send best lap time just after start lap
        {
            timerCanva.SetBestLap(lapTime);
        }
        if (other.gameObject.CompareTag("finishLine"))
        {
            numLaps++;
            timerCanva.AddLap();

            float point = (20 - lapTime) * lapTimeMultiplier;
            if (point < 0)  point = 0;


            lapPoints += point;

            lapTime = 0;


        }
    }

    private void Update()
    {
        lapTime += Time.deltaTime;

        //In case multiple cars just show lap time of first 
        if (setTime) timerCanva.SetActualLapTime(lapTime);
    }

    private void FixedUpdate()
    {
        //Get input of the neural network
        transform.position = new Vector3(transform.position.x, startPosition.y, transform.position.z);
        InputSensors();
        lastPosition = transform.position;

        //Move car
        MoveCar();

        lifeTime += Time.deltaTime;

        //Update fitness
        CalculateFitness();
    }

    private void Death ()
    {
        geneticManager.Death(overallFitness, this);
    }

    private void CalculateFitness() {

        distancetravelled += Vector3.Distance(transform.position,lastPosition);
        avgSpeed = distancetravelled / lifeTime;

       overallFitness = (distancetravelled * distanceMultipler) + (avgSpeed * avgSpeedMultiplier) +
            (((rightSensor + rightMidSensor + frontSensor + leftMidSensor +leftSensor) /3) * sensorMultiplier + (cornerValue * cornerMultiplier) + 
            lapPoints);

        if (lifeTime > maxlifeTime) {
            Death();
        }

        if (numLaps > 10) // Impossible to achive that in the lifetime means he is turning around himself
        {
            overallFitness /= 100; //Make sure is not in the best cars and is not the worst car
            Death();
        }

    }

    //Check distance to the wall from the center of the car
    private void InputSensors() {

        int layerMask = 1 << LayerMask.NameToLayer("Default");

        Vector3 r = (transform.forward+transform.right);
        Vector3 m = (transform.forward);
        Vector3 l = (transform.forward-transform.right);
        Vector3 rm = (transform.forward + transform.right);
        Vector3 lm = (transform.forward - transform.right);



        Ray rR= new Ray(transform.position,r);
        RaycastHit hitA;

        Ray rM = new Ray(transform.position, m);
        RaycastHit hitB;

        Ray rL = new Ray(transform.position, l);
        RaycastHit hitC;

        Ray rRM = new Ray(transform.position, rm);
        RaycastHit hitD;

        Ray rLM = new Ray(transform.position, lm);
        RaycastHit hitE;


        float distance = 0; //Max float
        int n = 0;

        Vector3 distA = new Vector3(), distB = new Vector3(), distC = new Vector3();

        if (Physics.Raycast(rR, out hitA, maxDistance, layerMask)) {
            rightSensor = hitA.distance/ maxDistance;

            distance = hitA.distance;
            if (rightSensor > 1)
            {
                rightSensor = 1;

                //Calculate far point
                Vector3 dir = (hitA.point - rR.origin).normalized;
                distA = hitA.point + dir * maxDistance;
            }
            else
            {
                distA = hitA.point;
            }
        }

        if (Physics.Raycast(rM, out hitB, maxDistance, layerMask)) {
            frontSensor = hitB.distance/ maxDistance;

            if (distance < hitB.distance)
            {
                distance = hitB.distance;
                n = 1;
            }

            if (frontSensor > 1)
            {
                frontSensor = 1;

                //Calculate far point
                Vector3 dir = (hitB.point - rM.origin).normalized;
                distB = hitA.point + dir * maxDistance;
            }
            else
            {
                distB = hitB.point;
            }
        }

        if (Physics.Raycast(rL, out hitC, maxDistance, layerMask))
        {
            leftSensor = hitC.distance / maxDistance;

            if (distance < hitC.distance)
            {
                distance = hitC.distance;
                n = 2;
            }

            if (leftSensor > 1)
            {
                leftSensor = 1;

                //Calculate far point
                Vector3 dir = (hitC.point - rL.origin).normalized;
                distC = hitC.point + dir * maxDistance;
            }
            else
            {
                distC = hitC.point;
            }
        }

        if (Physics.Raycast(rRM, out hitD, maxDistance, layerMask))
        {
            rightMidSensor = hitD.distance / maxDistance;

            if (rightMidSensor > 1)
            {
                rightMidSensor = 1;
            }
        }

        if (Physics.Raycast(rLM, out hitE, maxDistance, layerMask))
        {
            leftMidSensor = hitE.distance / maxDistance;

            if (leftMidSensor > 1)
            {
                leftMidSensor = 1;
            }
        }
    }

    private void MoveCar ()
    {        
        //Get movement
        (acceleration, rotation) = network.RunNetwork(rightSensor, rightMidSensor, frontSensor, leftMidSensor, leftSensor);

        input = Vector3.Lerp(Vector3.zero,new Vector3(0,0,acceleration* speed), Time.deltaTime);
        input = transform.TransformDirection(input);
        transform.position += input;


        transform.eulerAngles += new Vector3(0, (rotation * 90) * Time.deltaTime, 0);

        transform.position  = new Vector3(transform.position.x, startPosition.y, transform.position.z);
    }

}
