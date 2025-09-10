using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Barracuda;
using System.Collections.Concurrent;

public class VoiceProcessor : MonoBehaviour
{
    public ControlManager controllManager;
    [Header("Model Settings")]
    public NNModel modelAsset;
    public AudioRecorder audioRecorder;
    private Model _runtimeModel;
    private IWorker _worker;

    [Header("Command Settings")]
    public float confidenceThreshold = 0.7f;
    public float commandCooldown = 3f;
    private float _lastCommandTime;
    private string[] _commands = {"stop", "left", "go", "down", "right", "up"};

    // Start is called before the first frame update
    void Start()
    {
        _runtimeModel = ModelLoader.Load(modelAsset);
        _worker = WorkerFactory.CreateWorker(_runtimeModel);
        Debug.Log("Model loaded");
    }

    // Update is called once per frame
    void Update()
    {
        if (audioRecorder.audioQueue.TryDequeue(out float[] audio))
        {
            // if (audioRecorder.audioQueue.Count > 0)
            // {
            //     Debug.Log($"Audio queue size: {audioRecorder.audioQueue.Count}");
            // }
            // else
            // {
            //     Debug.Log("Audio queue empty");
            // }
            // Debug.Log($"Audio received: {audio.Length} samples");
    
            // Add audio validation
            float sum = 0f;
            foreach (float s in audio) sum += Mathf.Abs(s);
            float energy = sum / audio.Length;
    
            // Voice Activity Detection (VAD)
            float vadThreshold = 0.05f;
            if (energy < vadThreshold)
            {
                Debug.Log("No voice detected, skipping processing.");
                return;
            }
    
            // Debug.Log($"Processing audio: {audio.Length} samples");
            
            // Preprocess
            float[] mfcc = MFCCConverter.Convert(audio);
            float[,,,] inputData = ReshapeMFCC(mfcc);
            
            // Inference
            Tensor inputTensor = new Tensor(1, 63, 40, 1, inputData);
            _worker.Execute(inputTensor);
            Tensor outputTensor = _worker.PeekOutput();
            
            // Get results
            float[] probabilities = outputTensor.AsFloats();
            LogPredictions(probabilities);
            string command = GetCommand(probabilities);
            
            if (command != null && Time.time - _lastCommandTime >= commandCooldown)
            {
                controllManager.MoveCharacter(command);
                _lastCommandTime = Time.time; // Update the last command time
            }
            
            // Cleanup
            inputTensor.Dispose();
            outputTensor.Dispose();
        }
    }

    float[,,,] ReshapeMFCC(float[] mfcc)
    {
        int targetLength = 63 * 40;
        float[] paddedMfcc = new float[targetLength];
    
        // Copy and pad/truncate the MFCC array
        for (int i = 0; i < targetLength; i++)
        {
            if (i < mfcc.Length)
                paddedMfcc[i] = mfcc[i];
            else
                paddedMfcc[i] = 0; // Padding with zeros
        }
    
        float[,,,] data = new float[1, 63, 40, 1];
        for (int t = 0; t < 63; t++)
            for (int c = 0; c < 40; c++)
                data[0, t, c, 0] = paddedMfcc[t * 40 + c];
        return data;
    }

    void LogPredictions(float[] probs)
    {
        string log = "Voice Command Probabilities:\n";
        for (int i = 0; i < _commands.Length; i++)
            log += $"{_commands[i]}: {probs[i]:0.000}\n";
        Debug.Log(log);
    }

    string GetCommand(float[] probs)
    {
        int maxIndex = 0;
        float maxProb = probs[0];

        // Find the command with the highest probability
        for (int i = 1; i < probs.Length; i++)
        {
            if (probs[i] > maxProb)
            {
                maxProb = probs[i];
                maxIndex = i;
            }
        }

        // Return the command if it exceeds the confidence threshold
        if (maxProb >= confidenceThreshold)
        {
            return _commands[maxIndex];
        }
        else
        {
            Debug.Log("No command detected with sufficient confidence.");
            return null;
        }
    }

    void OnDestroy()
    {
        _worker?.Dispose();
    }
}