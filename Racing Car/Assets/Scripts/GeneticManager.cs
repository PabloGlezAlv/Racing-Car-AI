using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using UnityEngine;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.Distributions;

public class GeneticManager : MonoBehaviour
{
    [Header("Racing cars")]
    [SerializeField] List<Movement> carMovement;

    [Header("Genetic controls")]
    [SerializeField] int startPopulation = 85;
    [SerializeField] float mutationRate = 0.055f;
    [SerializeField] int elitistAgent = 1;

    [Header("Crossover controls")]
    [SerializeField] int bestAgent = 8;
    [SerializeField] int worstAgent = 3;
    [SerializeField] int nToCrossOver = 39;

    [Header("Application state")]
    [SerializeField] int actualGeneration = 0;
    [SerializeField] int actualGenome = 0;

    [Header("Network Configuration")]
    [SerializeField] int numLayers = 1;
    [SerializeField] int numNeurons = 10;

    private List<int> carSelection = new List<int>();

    private int naturalSelected;

    private NeuralNetwork[] netPopulation;

    private void Awake()
    {
        netPopulation = new NeuralNetwork[startPopulation];
        FillRandom(netPopulation, 0);

        for (actualGenome = 0; actualGenome < carMovement.Count; actualGenome++)
        {
            carMovement[actualGenome].ResetWithNetwork(netPopulation[actualGenome]);
        }
    }

    private void FillRandom(NeuralNetwork[] newPopulation, int startingIndex)
    {
        for (int i = startingIndex; i < newPopulation.Length; i++)
        {
            newPopulation[i] = new NeuralNetwork();
            newPopulation[i].Initialise(numLayers, numNeurons);
        }
    }

    private int findCar(Movement car)
    {
        for (int i = 0; i < carMovement.Count; i++)
        {
            if (carMovement[i] == car) return i;
        }

        return -1;
    }

    //Create next generation or keep with the same
    public void Death(float fitness, Movement car)
    {
        int number = findCar(car);
        if (number < 0)
        {
            Debug.LogError("Car not found in the network");
        }
        //Go to next car in generation in case is not finished
        if (actualGenome < netPopulation.Length - 1)
        {
            netPopulation[actualGenome++].setFitness(fitness);
        }
        else //Generation completed, create a new generation
        {
            NewGeneration();
        }

        carMovement[number].ResetWithNetwork(netPopulation[actualGenome]);
    }


    private void NewGeneration()
    {
        carSelection.Clear();
        actualGeneration++;
        naturalSelected = 0;

        // Sort cars to get the best ones
        QuickSort(1,netPopulation.Length - 1);

        NeuralNetwork[] newPopulation = Selection();
        Crossover(newPopulation);
        Mutation(newPopulation);

        //Complete the generation if possible with random cars
        FillRandom(newPopulation, naturalSelected);

        netPopulation = newPopulation;

        actualGenome = 0;
    }

    private void Mutation(NeuralNetwork[] newPopulation)
    {
        // Mutate gens excluding elitist ones
        for (int i = elitistAgent; i < naturalSelected; i++)
        {
            for (int j = 0; j < numLayers + 2; j++)
            {
                //Mutate only in a ratio 
                if (Random.Range(0.0f, 1.0f) < mutationRate)
                {
                    newPopulation[i].setWeight(j, MutateMatrix(newPopulation[i].getWeight(j)));
                }

            }
        }
    }

    Matrix<float> MutateMatrix(Matrix<float> m)
    {

        int randomPoints = Random.Range(1, (m.RowCount * m.ColumnCount) / 6);

        Matrix<float> mutatedMatrix = m;

        // Simple inversion mutation (SIM)
        for (int i = 0; i < randomPoints; i++)
        {
            int randomColumn1 = Random.Range(0, mutatedMatrix.ColumnCount);
            int randomRow1 = Random.Range(0, mutatedMatrix.RowCount);

            int randomColumn2;
            int randomRow2;
            do
            {
                randomColumn2 = Random.Range(0, mutatedMatrix.ColumnCount);
                randomRow2 = Random.Range(0, mutatedMatrix.RowCount);
            } while (randomColumn2 == randomColumn1 && randomRow1 == randomRow2);


            mutatedMatrix[randomRow1, randomColumn2] = mutatedMatrix[randomRow2, randomColumn2];
        }

        return mutatedMatrix;

    }

    // PMX
    // One fragment from one parent the rest from the other 
    private void Crossover(NeuralNetwork[] newPopulation)
    {
        for (int i = 0; i < nToCrossOver; i += 2)
        {
            int parent1 = i;
            int parent2 = i + 1;
            //Select parents
            if (carSelection.Count >= 1)
            {
                do
                {
                    parent1 = carSelection[Random.Range(0, carSelection.Count)];
                    parent2 = carSelection[Random.Range(0, carSelection.Count)];

                } while (parent1 != parent2);
            }

            NeuralNetwork child1 = new();
            NeuralNetwork child2 = new();

            child1.Initialise(numLayers, numNeurons);
            child2.Initialise(numLayers, numNeurons);

            child1.setFitness(0);
            child2.setFitness(0);


            int minValue = Random.Range(0, numLayers + 1);
            int maxValue = Random.Range(minValue, numLayers + 2);

            for (int j = 0; j < numLayers + 2; j++)
            {
                // Exchange weights if values are in the range
                if (j >= minValue && j <= maxValue)
                {
                    child1.setWeight(j, netPopulation[parent1].getWeight(j));
                    child2.setWeight(j, netPopulation[parent2].getWeight(j));
                }
                else
                {
                    child2.setWeight(j, netPopulation[parent1].getWeight(j));
                    child1.setWeight(j, netPopulation[parent2].getWeight(j));
                }
            }

            minValue = Random.Range(0, numLayers);
            maxValue = Random.Range(minValue, numLayers + 1);

            for (int j = 0; j < (numLayers + 1); j++)
            {
                // Exchange biases if values are in the range
                if (j >= minValue && j <= maxValue)
                {
                    child1.setBias(j, netPopulation[parent1].getBias(j));
                    child2.setBias(j, netPopulation[parent2].getBias(j));
                }
                else
                {
                    child2.setBias(j, netPopulation[parent1].getBias(j));
                    child1.setBias(j, netPopulation[parent2].getBias(j));
                }

            }

            newPopulation[naturalSelected++] = child1;
            newPopulation[naturalSelected++] = child2;
        }
    }

    private NeuralNetwork[] Selection()
    {

        NeuralNetwork[] newPopulation = new NeuralNetwork[startPopulation];

        //Pick best cars of the generation
        for (int i = 0; i < bestAgent; i++)
        {
            newPopulation[naturalSelected] = netPopulation[i].InitCopy(numLayers, numNeurons);
            newPopulation[naturalSelected++].setFitness(0);

            int f = Mathf.RoundToInt(netPopulation[i].getFitness() * 10);

            for (int c = 0; c < f; c++)
            {
                carSelection.Add(i);
            }

        }

        //Pick worst cars of the generation
        for (int i = 0; i < worstAgent; i++)
        {
            int last = netPopulation.Length - 1;
            last -= i;

            int f = Mathf.RoundToInt(netPopulation[last].getFitness() * 10);

            for (int c = 0; c < f; c++)
            {
                carSelection.Add(last);
            }

        }

        return newPopulation;

    }

    private void QuickSort(int low, int high) //O (n log n)
    {
        if (low < high)
        {
            int partitionIndex = Partition( low, high);

            QuickSort(low, partitionIndex - 1);
            QuickSort( partitionIndex + 1, high);
        }
    }

    private int Partition( int low, int high)
    {
        NeuralNetwork pivot = netPopulation[high];
        int i = low - 1;

        for (int j = low; j < high; j++)
        {
            if (netPopulation[j].getFitness() >= pivot.getFitness())
            {
                i++;

                NeuralNetwork temp = netPopulation[i];
                netPopulation[i] = netPopulation[j];
                netPopulation[j] = temp;
            }
        }

        NeuralNetwork temp2 = netPopulation[i + 1];
        netPopulation[i + 1] = netPopulation[high];
        netPopulation[high] = temp2;
        return i + 1;
    }
}
