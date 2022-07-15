using Godot;
using System;

public class MicrophoneRecorder : Node2D
{
    private AudioEffectRecord _microphoneRecord;            // The microphone recording bus effect
    private AudioStreamSample _recordedSample;              // Recorded audio as stream sample
    private AudioStreamPlayer _audioStreamPlayer;           // The Audio Stream Player
    public Vector2 MicrophoneInputPeakVolume;               // The microphone input peak volume

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        // Get the record bus
        _microphoneRecord = AudioServer.GetBusEffect(AudioServer.GetBusIndex("Record"), 0) as AudioEffectRecord;
        // Get the audio stream player node
        _audioStreamPlayer = GetNode<AudioStreamPlayer>("AudioStreamPlayer");
    }
    private void GetLeftAndRightChannelInputPeakVolumes()
    {
        MicrophoneInputPeakVolume.x = AudioServer.GetBusPeakVolumeLeftDb(AudioServer.GetBusIndex("Record"), 0);
        MicrophoneInputPeakVolume.y = AudioServer.GetBusPeakVolumeRightDb(AudioServer.GetBusIndex("Record"), 0);
    }

    private void DrawMicrophoneInputPeakVolumes()
    {
        // Draw the peak volume boxes
        DrawRect(new Rect2(280.0f,480.0f,20.0f,-90), new Color(0,0,0,1),filled:false);
        DrawRect(new Rect2(310.0f,480.0f,20.0f,-90), new Color(0,0,0,1),filled:false);
        // Draw the peak volume bars
        DrawRect(new Rect2(280.0f,480.0f,20.0f,-90+Mathf.Abs(MicrophoneInputPeakVolume.x)), new Color(1,1,1,1),filled:true);
        DrawRect(new Rect2(310.0f,480.0f,20.0f,-90+Mathf.Abs(MicrophoneInputPeakVolume.y)), new Color(1,1,1,1),filled:true);
    }

    public override void _Draw()
    {
        DrawMicrophoneInputPeakVolumes();
    }

    public override void _Process(float delta)
    {
        // Get the left and right channels
        GetLeftAndRightChannelInputPeakVolumes();

        // Call update so _Draw() will be run again
        Update();
    }

    private void OnButtonToggled(bool buttonPressed)
    {
        if(buttonPressed)
        {
            // If the microphone is not recording
            if(!_microphoneRecord.IsRecordingActive())
            {
                _microphoneRecord.SetRecordingActive(true);                   // Start the the microphone recording
            }
            GetNode<Button>("RecordButton").Text = "Stop Recording";          // Set the text on the button to "Stop Recording"

            // Set a red tone to the button when the user starts to record
            GetNode<Button>("RecordButton").Modulate = new Color(1f,0.5f,0.5f,1f);
        }
        else
        {
            GetNode<Button>("RecordButton").Text = "Record";                  // Set the text on the button to "Record"
            GetNode<Button>("RecordButton").Modulate = new Color(1,1,1,1f);   // Restore the button colors to normal
            _microphoneRecord.SetRecordingActive(false);                      // Turn off microphone
            _recordedSample = _microphoneRecord.GetRecording();               // Get the microphone recording
            _recordedSample.Data = MixStereoToMono(_recordedSample.Data);     // Mix stero to mono
            _audioStreamPlayer.Stream = _recordedSample;                      // Set the audio player stream to the recording
            _audioStreamPlayer.Play();                                        // Play back what was said
        }
    }

    private byte[] MixStereoToMono(byte[] input)
    {
        // If the sample length can be divided by 4, it's a valid stero sound
        if(input.Length % 4 == 0)
        {
            byte[] output = new byte[input.Length / 2];                 // create a new byte array half the size of the stereo length
            int outputIndex = 0;
            for (int n = 0; n < input.Length; n+=4)                     // Loop through each stero sample
            {
                int leftChannel = BitConverter.ToInt16(input,n);        // Get the left channel
                int rightChannel = BitConverter.ToInt16(input,n+2);     // Get the right channel
                int mixed = (leftChannel + rightChannel) / 2;           // Mix them together
                byte[] outSample = BitConverter.GetBytes((short)mixed); // Convert mix to bytes

                // copy in the first 16 bit sample
                output[outputIndex++] = outSample[0];
                output[outputIndex++] = outSample[1];
            }
            return output;
        }
        else
        {
            // Sound buffer is not a valid stere sound - Create empty sound for playback
            //to not crash the game/application
            byte[] output = new byte[24];

            // Return silence
            return output;
        }
    }
}
