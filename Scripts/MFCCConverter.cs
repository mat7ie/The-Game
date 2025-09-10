using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MathNet.Numerics;
using MathNet.Numerics.IntegralTransforms;

public static class MFCCConverter
{
    public static float[] Convert(float[] audio)
    {
        int sampleRate = 16000;
        int nMfcc = 40;
        int nFft = 512;
        int hopLength = 256;

        // 1. Pre-emphasis
        for (int i = 1; i < audio.Length; i++)
            audio[i] = audio[i] - 0.97f * audio[i - 1];

        // 2. Frame the signal into short frames
        List<float[]> frames = FrameSignal(audio, nFft, hopLength);

        // 3. Apply FFT and Mel filterbank to each frame
        List<float[]> melSpectrogram = new List<float[]>();
        foreach (var frame in frames)
        {
            Complex32[] fftBuffer = new Complex32[nFft];
            for (int i = 0; i < frame.Length; i++)
                fftBuffer[i] = new Complex32(frame[i], 0);
            Fourier.Forward(fftBuffer);

            float[] melSpectrum = ApplyMelFilterbank(fftBuffer, sampleRate, nFft);
            melSpectrogram.Add(melSpectrum);
        }

        // 4. Compute MFCCs from Mel spectrogram
        List<float[]> mfccList = new List<float[]>();
        foreach (var melSpectrum in melSpectrogram)
        {
            float[] mfcc = ComputeDCT(melSpectrum, nMfcc);
            mfccList.Add(mfcc);
        }

        // Flatten the MFCCs to a 1D array
        float[] mfccArray = Flatten(mfccList);

        return mfccArray;
    }

    static List<float[]> FrameSignal(float[] signal, int frameSize, int hopLength)
    {
        List<float[]> frames = new List<float[]>();
        for (int i = 0; i < signal.Length - frameSize; i += hopLength)
        {
            float[] frame = new float[frameSize];
            System.Array.Copy(signal, i, frame, 0, frameSize);
            frames.Add(frame);
        }
        return frames;
    }

    static float[] ComputeDCT(float[] spectrum, int nCepstral)
    {
        float[] cepstralCoefficients = new float[nCepstral];
        int N = spectrum.Length;

        for (int k = 0; k < nCepstral; k++)
        {
            float sum = 0.0f;
            for (int n = 0; n < N; n++)
            {
                sum += spectrum[n] * Mathf.Cos(Mathf.PI * k * (2 * n + 1) / (2 * N));
            }
            cepstralCoefficients[k] = sum;
        }

        return cepstralCoefficients;
    }

    static float[] ApplyMelFilterbank(Complex32[] fftBuffer, int sampleRate, int nFft)
    {
        int numFilters = 26;
        float[] melSpectrum = new float[numFilters];

        // Define mel filterbank parameters
        float minHz = 0;
        float maxHz = 8000;
        float minMel = HzToMel(minHz);
        float maxMel = HzToMel(maxHz);
        float[] melPoints = new float[numFilters + 2];
        for (int i = 0; i < melPoints.Length; i++)
        {
            melPoints[i] = minMel + (maxMel - minMel) * i / (numFilters + 1);
        }

        float[] bin = new float[melPoints.Length];
        for (int i = 0; i < melPoints.Length; i++)
        {
            bin[i] = Mathf.Floor((nFft + 1) * MelToHz(melPoints[i]) / sampleRate);
        }

        // Apply mel filterbank
        for (int i = 1; i < numFilters + 1; i++)
        {
            for (int j = (int)bin[i - 1]; j < (int)bin[i]; j++)
            {
                melSpectrum[i - 1] += (j - bin[i - 1]) / (bin[i] - bin[i - 1]) * fftBuffer[j].Magnitude;
            }
            for (int j = (int)bin[i]; j < (int)bin[i + 1]; j++)
            {
                melSpectrum[i - 1] += (bin[i + 1] - j) / (bin[i + 1] - bin[i]) * fftBuffer[j].Magnitude;
            }
        }

        return melSpectrum;
    }

    static float HzToMel(float hz)
    {
        return 2595 * Mathf.Log10(1 + hz / 700);
    }

    static float MelToHz(float mel)
    {
        return 700 * (Mathf.Pow(10, mel / 2595) - 1);
    }

    static float[] Flatten(List<float[]> list)
    {
        List<float> flattened = new List<float>();
        foreach (var array in list)
        {
            flattened.AddRange(array);
        }
        return flattened.ToArray();
    }
}