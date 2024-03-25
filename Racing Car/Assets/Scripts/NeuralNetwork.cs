using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using MathNet.Numerics.LinearAlgebra;
using System;


// No license found
//
//    Adapted from this video: https://www.youtube.com/watch?v=C6SZUU8XQQ0&t=1257s&ab_channel=AJTech
//
//--------------------------------------------
// Author: AJTech

public class NeuralNetwork : MonoBehaviour
{
    // 5 sensors in the car
    private Matrix<float> inputLayer = Matrix<float>.Build.Dense(1, 5);

    private List<Matrix<float>> hiddenLayers = new();

    // Acceleration and rotation
    private Matrix<float> outputLayer = Matrix<float>.Build.Dense(1, 2);

    private List<Matrix<float>> weights = new();

    private List<float> layerBias = new();

    private float fitness;
    public void setWeight(int i, Matrix<float> value)
    {
        weights[i] = value;
    }

    public Matrix<float> getWeight(int i)
    {
        return weights[i];
    }

    public void setBias(int i, float value)
    {
        layerBias[i] = value;
    }

    public float getBias(int i)
    {
        return layerBias[i];
    }

    private void Clear()
    {
        inputLayer.Clear();
        hiddenLayers.Clear();
        outputLayer.Clear();
        weights.Clear();
        layerBias.Clear();
    }

    public float getFitness()
    {
        return fitness;
    }

    public void setFitness(float newFitness)
    {
        fitness = newFitness;
    }

    public void Initialise(int nLayers, int nNeurons)
    {
        Clear();

        // First layer
        Matrix<float> layer = Matrix<float>.Build.Dense(1, nNeurons);
        //Add layer
        hiddenLayers.Add(layer);

        //Add weights
        Matrix<float> inputToH1 = Matrix<float>.Build.Dense(inputLayer.ColumnCount, nNeurons);
        weights.Add(inputToH1);
        Matrix<float> hiddenToHidden = Matrix<float>.Build.Dense(nNeurons, nNeurons);
        weights.Add(hiddenToHidden);

        //Add bias
        layerBias.Add(UnityEngine.Random.Range(-1f, 1f));

        // Rest of layers
        for (int i = 1; i < nLayers; i++)
        {
            //Add layer
            layer = Matrix<float>.Build.Dense(1, nNeurons);
            hiddenLayers.Add(layer);

            //Add weights
            hiddenToHidden = Matrix<float>.Build.Dense(nNeurons, nNeurons);
            weights.Add(hiddenToHidden);

            //Add bias
            layerBias.Add(UnityEngine.Random.Range(-1f, 1f));
        }

        //Add layer
        Matrix<float> outputWeight = Matrix<float>.Build.Dense(nNeurons, outputLayer.ColumnCount);
        
        //Add weights
        weights.Add(outputWeight);
        
        //Add bias
        layerBias.Add(UnityEngine.Random.Range(-1f, 1f));

        RandomWeight();
    }

    private void RandomWeight()
    {
        for (int i = 0; i < weights.Count; i++)
        {
            for (int j = 0; j < weights[i].RowCount; j++)
            {
                for (int k = 0; k < weights[i].ColumnCount; k++)
                {
                    weights[i][j, k] = UnityEngine.Random.Range(-1f, 1f);
                }
            }
        }
    }

    public NeuralNetwork InitCopy(int hiddenLayerCount, int hiddenNeuronCount)
    {
        NeuralNetwork n = new NeuralNetwork();

        List<Matrix<float>> newWeights = new List<Matrix<float>>();

        for (int i = 0; i < this.weights.Count; i++)
        {
            newWeights.Add(getWeightMatrix(i));
        }

        List<float> newBiases = new();

        newBiases.AddRange(layerBias);

        n.weights = newWeights;
        n.layerBias = newBiases;

        n.InitHidden(hiddenLayerCount, hiddenNeuronCount);
        return n;
    }

    private Matrix<float> getWeightMatrix(int nMatrix)
    {
        Matrix<float> mWeights = Matrix<float>.Build.Dense(weights[nMatrix].RowCount, weights[nMatrix].ColumnCount);

        for (int j = 0; j < mWeights.RowCount; j++)
        {
            for (int k = 0; k < mWeights.ColumnCount; k++)
            {
                mWeights[j, k] = weights[nMatrix][j, k];
            }
        }

        return mWeights;
    }

    private void InitHidden(int hiddenLayerCount, int hiddenNeuronCount)
    {
        inputLayer.Clear();
        hiddenLayers.Clear();
        outputLayer.Clear();

        for (int i = 0; i < hiddenLayerCount + 1; i++)
        {
            Matrix<float> newHiddenLayer = Matrix<float>.Build.Dense(1, hiddenNeuronCount);
            hiddenLayers.Add(newHiddenLayer);
        }
    }
    public (float, float) RunNetwork(float r, float rm, float m, float lm, float l)
    {
        inputLayer[0, 0] = r;
        inputLayer[0, 1] = rm;
        inputLayer[0, 2] = m;
        inputLayer[0, 3] = lm;
        inputLayer[0, 4] = l;

        //Fist layer result
        hiddenLayers[0] = ((inputLayer * weights[0]) + layerBias[0]);

        //Hidden layers result
        for (int i = 1; i < hiddenLayers.Count; i++)
        {
            hiddenLayers[i] = ((hiddenLayers[i - 1] * weights[i]) + layerBias[i]);
        }

        //Output result
        outputLayer = ((hiddenLayers[hiddenLayers.Count - 1] * weights[weights.Count - 1]) + layerBias[layerBias.Count - 1]);

        outputLayer = outputLayer.PointwiseTanh(); //Limit Range

        return (Sigmoid(outputLayer[0, 0]), (float)Math.Tanh(outputLayer[0, 1]));
    }

    private float Sigmoid(float s)
    {
        return (1 / (1 + Mathf.Exp(-s)));
    }

}

