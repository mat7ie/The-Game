using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Collections.Concurrent;

public class AudioRecorder : MonoBehaviour
{
    [Header("Audio Settings")]
    public int sampleRate = 16000;
    public float clipLength = 1f; // Seconds
    public ConcurrentQueue<float[]> audioQueue = new ConcurrentQueue<float[]>();

    private AudioClip _microphoneClip;
    private bool _isRecording;
    // Start is called before the first frame update
    void Start()
    {
        StartMicrophone();
    }

    void StartMicrophone()
    {
        // if (Microphone.devices.Length == 0)
        // {
        //     Debug.LogError("No microphone found!");
        //     return;
        // }

        _microphoneClip = Microphone.Start(null, true, Mathf.CeilToInt(clipLength), sampleRate);
        // if (_microphoneClip == null)
        // {
        //     Debug.LogError("Failed to start microphone!");
        //     return;
        // }

        _isRecording = true;
        // Debug.Log("Microphone started");
        // Debug.Log($"Microphone state: {Microphone.IsRecording(null)}");
        // Debug.Log($"Clip samples: {_microphoneClip.samples}");
    }

    // Update is called once per frame
    void Update()
    {
        if (!_isRecording) return;
        
        int position = Microphone.GetPosition(null);
        int bufferSize = sampleRate * (int)clipLength;
    
        // Capture audio even if buffer isn't full
        if (position > 0)
        {
            float[] samples = new float[bufferSize];
            int startPos = position - bufferSize;
    
            if (startPos < 0)
            {
                // Handle wrap-around
                int firstPartLength = bufferSize + startPos;
                float[] firstPart = new float[firstPartLength];
                float[] secondPart = new float[-startPos];
    
                _microphoneClip.GetData(firstPart, 0);
                _microphoneClip.GetData(secondPart, _microphoneClip.samples + startPos);
    
                System.Array.Copy(firstPart, 0, samples, 0, firstPartLength);
                System.Array.Copy(secondPart, 0, samples, firstPartLength, secondPart.Length);
            }
            else
            {
                _microphoneClip.GetData(samples, startPos);
            }
    
            audioQueue.Enqueue(samples);
            // Debug.Log($"Audio captured: {samples.Length} samples");
        }
    }

    void OnDestroy()
    {
        Microphone.End(null);
    }
}
