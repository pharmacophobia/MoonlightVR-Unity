// =============================================================================
// MoonlightVR for Meta Quest 3
// Copyright (C) 2026 Pharmacophobia & Contributors
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <https://www.gnu.org/licenses/>.
// =============================================================================

package com.pharmacophobia.moonlightvr;

import android.graphics.SurfaceTexture;
import android.media.MediaCodec;
import android.media.MediaFormat;
import android.opengl.GLES11Ext;
import android.opengl.GLES20;
import android.os.Build;
import android.util.Log;
import android.view.Surface;

import java.io.IOException;
import java.nio.ByteBuffer;

/**
 * Native Android hardware video decoder bridge using MediaCodec and SurfaceTexture.
 * Delivers zero-copy hardware decoding of H.264 / HEVC video frames on Snapdragon XR2 Gen 2 (Meta Quest 3).
 */
public class MoonlightDecoderBridge {
    private static final String TAG = "MoonlightDecoderBridge";

    private static SurfaceTexture sSurfaceTexture;
    private static Surface sSurface;
    private static MediaCodec sMediaCodec;
    private static int sTextureId = 0;
    private static boolean sIsRunning = false;

    /**
     * Allocates an OpenGL external texture and binds it to a SurfaceTexture for Unity.
     * Returns the GL texture ID to be consumed by Unity's Texture2D.CreateExternalTexture.
     */
    public static int createDecoderSurfaceTexture(int width, int height) {
        try {
            int[] textures = new int[1];
            GLES20.glGenTextures(1, textures, 0);
            sTextureId = textures[0];

            GLES20.glBindTexture(GLES11Ext.GL_TEXTURE_EXTERNAL_OES, sTextureId);
            GLES20.glTexParameteri(GLES11Ext.GL_TEXTURE_EXTERNAL_OES, GLES20.GL_TEXTURE_MIN_FILTER, GLES20.GL_LINEAR);
            GLES20.glTexParameteri(GLES11Ext.GL_TEXTURE_EXTERNAL_OES, GLES20.GL_TEXTURE_MAG_FILTER, GLES20.GL_LINEAR);
            GLES20.glTexParameteri(GLES11Ext.GL_TEXTURE_EXTERNAL_OES, GLES20.GL_TEXTURE_WRAP_S, GLES20.GL_CLAMP_TO_EDGE);
            GLES20.glTexParameteri(GLES11Ext.GL_TEXTURE_EXTERNAL_OES, GLES20.GL_TEXTURE_WRAP_T, GLES20.GL_CLAMP_TO_EDGE);

            sSurfaceTexture = new SurfaceTexture(sTextureId);
            sSurfaceTexture.setDefaultBufferSize(width, height);
            sSurface = new Surface(sSurfaceTexture);

            Log.i(TAG, "Initialized external texture ID " + sTextureId + " with Surface: " + width + "x" + height);
            return sTextureId;
        } catch (Exception e) {
            Log.e(TAG, "Error initializing SurfaceTexture: " + e.getMessage(), e);
            return 0;
        }
    }

    /**
     * Initializes MediaCodec with low-latency tuning flags for Snapdragon XR2 Gen 2.
     */
    public static boolean initializeDecoder(String mimeType, int width, int height) {
        try {
            sMediaCodec = MediaCodec.createDecoderByType(mimeType);
            MediaFormat format = MediaFormat.createVideoFormat(mimeType, width, height);
            
            // Enable low latency operating rate and real-time priorities
            format.setInteger(MediaFormat.KEY_PRIORITY, 0); // Realtime priority
            if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.R) {
                format.setInteger(MediaFormat.KEY_LOW_LATENCY, 1);
            }

            sMediaCodec.configure(format, sSurface, null, 0);
            sMediaCodec.start();
            sIsRunning = true;
            Log.i(TAG, "MediaCodec initialized successfully for " + mimeType);
            return true;
        } catch (IOException e) {
            Log.e(TAG, "Failed to initialize MediaCodec: " + e.getMessage(), e);
            return false;
        }
    }

    /**
     * Submits an encoded NAL unit byte buffer to MediaCodec.
     */
    public static void feedNalUnit(byte[] nalData, int offset, int length, long presentationTimeUs) {
        if (!sIsRunning || sMediaCodec == null) return;
        try {
            int inputBufferIndex = sMediaCodec.dequeueInputBuffer(2000); // 2ms timeout
            if (inputBufferIndex >= 0) {
                ByteBuffer inputBuffer = sMediaCodec.getInputBuffer(inputBufferIndex);
                if (inputBuffer != null) {
                    inputBuffer.clear();
                    inputBuffer.put(nalData, offset, length);
                    sMediaCodec.queueInputBuffer(inputBufferIndex, 0, length, presentationTimeUs, 0);
                }
            }
        } catch (Exception e) {
            Log.w(TAG, "Error feeding NAL unit: " + e.getMessage());
        }
    }

    /**
     * Updates the surface texture with the latest decoded frame.
     */
    public static void updateTexImage() {
        if (sSurfaceTexture != null) {
            try {
                sSurfaceTexture.updateTexImage();
            } catch (Exception e) {
                // Ignore dropped frame or concurrent update warning
            }
        }
    }

    /**
     * Releases hardware resources.
     */
    public static void release() {
        sIsRunning = false;
        if (sMediaCodec != null) {
            try {
                sMediaCodec.stop();
                sMediaCodec.release();
            } catch (Exception ignored) {}
            sMediaCodec = null;
        }
        if (sSurface != null) {
            sSurface.release();
            sSurface = null;
        }
        if (sSurfaceTexture != null) {
            sSurfaceTexture.release();
            sSurfaceTexture = null;
        }
    }
}
