﻿using BepInEx;
using BepInEx.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;

namespace FivePebblesBadApple
{
    public static class EnumExt_FPBA
    {
        public static SoundID Bad_Apple;
        public static SSOracleBehavior.Action Degeneracy_BadApple;
    }


    [BepInPlugin("forthbridge.five_pebbles_bad_apple", "FivePebblesBadApple", "0.1.0")]
    public class FivePebblesBadApple : BaseUnityPlugin
    {
        #region BepInEx Logger
        // Allows access to BepInEx logger from other classes, see: https://rainworldmodding.miraheze.org/wiki/Code_Environments
        private static WeakReference self;
        public static FivePebblesBadApple SELF => self?.Target as FivePebblesBadApple;
        public ManualLogSource Logger_p => Logger;
        public FivePebblesBadApple() => self = new WeakReference(this);
        #endregion

        public static Dictionary<string, byte[]> frames = new Dictionary<string, byte[]>();
        public static AudioClip audio = new AudioClip();

        // The application of all hooks is delegated to a static class
        public void OnEnable() => Hooks.ApplyHooks();

        const string FRAMES_RESOURCE_PATH = "FivePebblesBadApple.Frames";
        const string AUDIO_RESOURCE_PATH = "FivePebblesBadApple.BadApple.mp3";

        // Credit to LB on the RW Discord!
        // Loads all frames into the 'frames' array, each frame is an embedded resource png located under the 'Frames' directory in the assembly
        public static void LoadFrames()
        {
            // Get all embedded resources from the assembly via reflection
            Assembly assembly = Assembly.GetExecutingAssembly();
            int index = 0;

            foreach (string resourceName in assembly.GetManifestResourceNames())
            {
                // Ensure that the resource is under the frames directory
                if (!resourceName.StartsWith(FRAMES_RESOURCE_PATH) && resourceName != AUDIO_RESOURCE_PATH) return;

                // Get both the resource stream from the resource name and declare a new memory stream
                using Stream resourceStream = assembly.GetManifestResourceStream(resourceName);
                using MemoryStream memoryStream = new MemoryStream();

                byte[] buffer = new byte[16384];
                int read;

                // Write the resource stream into a memory stream
                while ((read = resourceStream!.Read(buffer, 0, buffer.Length)) > 0) memoryStream.Write(buffer, 0, read);

                if (resourceName == AUDIO_RESOURCE_PATH)
                {
                    // Thanks to williamsenzo on the Unity Forums!

                    // The size of a float is 4 bytes
                    float[] samples = new float[memoryStream.ToArray().Length / 4];
                    Buffer.BlockCopy(memoryStream.ToArray(), 0, samples, 0, memoryStream.ToArray().Length);

                    int channels = 1; // Mono Audio
                    int sampleRate = 44100; // Standard CD Sample Rate

                    audio = AudioClip.Create("BadApple", samples.Length, channels, sampleRate, false, false);
                    audio.SetData(samples, 0);
                    return;
                }

                string frameName = "FPBadApple_" + index;

                // Finally, add the texture to the list of frames
                frames[frameName] = memoryStream.ToArray();
                index++;

                SELF.Logger_p.LogInfo("Loaded Frame " + resourceName + " as " + frameName);

                // Only load the first 500 frames to speed up loading when debugging
                // if (index > 500) break;
            }
        }
    }

    public class ProjectedImageFromMemory : ProjectedImage
    {
        public ProjectedImageFromMemory(List<Texture2D> textures, List<string> imageNames, int cycleTime) : base(imageNames, cycleTime)
        {
            if (textures == null || imageNames == null || textures.Count != imageNames.Count) return;

            for (int i = 0; i < textures.Count; i++)
            {
                if (Futile.atlasManager.GetAtlasWithName(imageNames[i]) != null) break;

                textures[i].wrapMode = TextureWrapMode.Clamp;
                textures[i].anisoLevel = 0;
                textures[i].filterMode = FilterMode.Point;
                Futile.atlasManager.LoadAtlasFromTexture(imageNames[i], textures[i]);
            }
        }
    }
}
