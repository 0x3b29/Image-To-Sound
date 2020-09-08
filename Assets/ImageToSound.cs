using System.Collections.Generic;
using UnityEngine;

// This code is heavily based by https://forum.unity.com/threads/generating-a-simple-sinewave.471529/ 
public class ImageToSound : MonoBehaviour
{
    [Range(1, 20000)]  //Creates a slider in the inspector
    public float frequency1;

    [Range(1, 20000)]  //Creates a slider in the inspector
    public float frequency2;

    [Range(0, 1)]
    public float grayscaleCutoff = .7f;

    [Range(1, 10)]
    public float maxLineReplay = 3;

    [Range(0, 5)]
    public float stretchWidth = 1.7f;

    [Range(0, 1000)]
    public int offset = 100;

    [Range(0, 1000)]
    public int spaceBetweenLoops = 100;

    public Texture2D texture2D;

    private float sampleRate = 44100;

    private List<int> selectedPixels = new List<int>();
    private AudioSource audioSource;

    void Start()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0; //force 2D sound
        audioSource.Stop(); //avoids audiosource from starting to play automatically
    }

    int currentLine = 0;
    int currentLineReplayedCounter = 0;
    int currentSpaceBetweenLoops = 0;

    // Update is called every new frame, for 30 fps, this means sqrt(1/30) ms
    // This results in aprox. every 18 ms which is good enough for the OnAudioFilterRead to interact with
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (!audioSource.isPlaying)
            {
                audioSource.Play();
            }
            else
            {
                audioSource.Stop();
            }
        }

        // Do not update lines when audio is not playing
        if (!audioSource.isPlaying)
        {
            return;
        }

        // To stretch an image vertically, we can display the same line for multiple frames. But if we do display the same line for longer, we might also choose new pixels from this line to display to have more detail in the final image
        if (currentLineReplayedCounter >= maxLineReplay)
        {
            if (currentLine > texture2D.height)
            {
                // Also add a few extra lines where nothing is happening between loops
                if (currentSpaceBetweenLoops < spaceBetweenLoops)
                {
                    frequency1 = 0;
                    frequency2 = 0;
                    currentSpaceBetweenLoops++;
                    return;
                }

                currentSpaceBetweenLoops = 0;
                currentLine = 0;
            }

            currentLine += 1;

            currentLineReplayedCounter = 0;
        }

        currentLineReplayedCounter++;

        // Save all the pixel indexes in a list which have a greyscale higher than the threshold
        selectedPixels.Clear();

        for (int i = 0; i < texture2D.width; i++)
        {
            if (texture2D.GetPixel(i, currentLine).grayscale < grayscaleCutoff)
            {
                selectedPixels.Add(i);
            }
        }

        // Then select random indexes from the list
        // We can only display one pixel per frame per channel.
        if (selectedPixels.Count == 0)
        {
            frequency1 = 0;
            frequency2 = 0;
        }
        else if (selectedPixels.Count == 1)
        {
            frequency1 = Mathf.Pow(selectedPixels[0], stretchWidth) + offset;
            frequency2 = 0;
        }
        else
        {
            frequency1 = Mathf.Pow(selectedPixels[Random.Range(0, selectedPixels.Count)], stretchWidth) + offset;
            frequency2 = Mathf.Pow(selectedPixels[Random.Range(0, selectedPixels.Count)], stretchWidth) + offset;
        }
    }

    // The phase variables keep track of of the vertical position of the wave
    private float phase1 = 0;
    private float phase2 = 0;

    // The unity doc states, OnAudioFilterRead is called +- every 20 ms
    void OnAudioFilterRead(float[] data, int channels)
    {
        for (int i = 0; i < data.Length; i += channels)
        {
            // Calculate the wave for the given frequency
            phase1 += 2 * Mathf.PI * frequency1 / sampleRate;
            data[i] = Mathf.Sin(phase1);

            // To avoid glitches, we need to make sure there are no jumps from negative to positve when we reiterate
            if (phase1 >= 2 * Mathf.PI)
            {
                phase1 -= 2 * Mathf.PI;
            }

            // If a second channel is present, do the same operations for this channel as well
            if (channels >= 2)
            {
                phase2 += 2 * Mathf.PI * frequency2 / sampleRate;
                data[i + 1] = Mathf.Sin(phase2);

                if (phase2 >= 2 * Mathf.PI)
                {
                    phase2 -= 2 * Mathf.PI;
                }
            }
        }
    }
}