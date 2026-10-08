package com.pharmacophobia.moonlightvr

import android.app.Activity
import android.os.Bundle
import android.util.Log
import android.view.WindowManager

/**
 * Main Activity for Moonlight VR on Meta Quest 3.
 * Configured with VR immersive flags and hardware decoding pipeline initialization.
 */
class MainActivity : Activity() {
    companion object {
        private const val TAG = "MoonlightVRActivity"
    }

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        Log.i(TAG, "Starting Moonlight VR on Meta Quest...")

        // Keep screen on during remote desktop streaming
        window.addFlags(WindowManager.LayoutParams.FLAG_KEEP_SCREEN_ON)
    }

    override fun onDestroy() {
        super.onDestroy()
        MoonlightDecoderBridge.release()
    }
}
