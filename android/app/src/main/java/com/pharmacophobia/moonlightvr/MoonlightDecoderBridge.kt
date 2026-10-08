package com.pharmacophobia.moonlightvr

import android.graphics.SurfaceTexture
import android.media.MediaCodec
import android.media.MediaFormat
import android.opengl.GLES11Ext
import android.opengl.GLES20
import android.os.Build
import android.util.Log
import android.view.Surface
import java.io.IOException
import java.nio.ByteBuffer

/**
 * Native Android hardware video decoder bridge using MediaCodec and SurfaceTexture.
 * Delivers zero-copy hardware decoding of H.264 / HEVC video frames on Snapdragon XR2 Gen 2 (Meta Quest 3).
 */
object MoonlightDecoderBridge {
    private const val TAG = "MoonlightDecoderBridge"

    private var sSurfaceTexture: SurfaceTexture? = null
    private var sSurface: Surface? = null
    private var sMediaCodec: MediaCodec? = null
    private var sTextureId: Int = 0
    private var sIsRunning: Boolean = false

    /**
     * Allocates an OpenGL external texture and binds it to a SurfaceTexture for Unity.
     * Returns the GL texture ID to be consumed by Unity's Texture2D.CreateExternalTexture.
     */
    @JvmStatic
    fun createDecoderSurfaceTexture(width: Int, height: Int): Int {
        return try {
            val textures = IntArray(1)
            GLES20.glGenTextures(1, textures, 0)
            sTextureId = textures[0]

            GLES20.glBindTexture(GLES11Ext.GL_TEXTURE_EXTERNAL_OES, sTextureId)
            GLES20.glTexParameteri(GLES11Ext.GL_TEXTURE_EXTERNAL_OES, GLES20.GL_TEXTURE_MIN_FILTER, GLES20.GL_LINEAR)
            GLES20.glTexParameteri(GLES11Ext.GL_TEXTURE_EXTERNAL_OES, GLES20.GL_TEXTURE_MAG_FILTER, GLES20.GL_LINEAR)
            GLES20.glTexParameteri(GLES11Ext.GL_TEXTURE_EXTERNAL_OES, GLES20.GL_TEXTURE_WRAP_S, GLES20.GL_CLAMP_TO_EDGE)
            GLES20.glTexParameteri(GLES11Ext.GL_TEXTURE_EXTERNAL_OES, GLES20.GL_TEXTURE_WRAP_T, GLES20.GL_CLAMP_TO_EDGE)

            sSurfaceTexture = SurfaceTexture(sTextureId).apply {
                setDefaultBufferSize(width, height)
            }
            sSurface = Surface(sSurfaceTexture)

            Log.i(TAG, "Initialized external texture ID $sTextureId with Surface: ${width}x$height")
            sTextureId
        } catch (e: Exception) {
            Log.e(TAG, "Error initializing SurfaceTexture: ${e.message}", e)
            0
        }
    }

    /**
     * Initializes MediaCodec with low-latency tuning flags for Snapdragon XR2 Gen 2.
     */
    @JvmStatic
    fun initializeDecoder(mimeType: String, width: Int, height: Int): Boolean {
        return try {
            sMediaCodec = MediaCodec.createDecoderByType(mimeType)
            val format = MediaFormat.createVideoFormat(mimeType, width, height).apply {
                setInteger(MediaFormat.KEY_PRIORITY, 0) // Realtime priority
                if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.R) {
                    setInteger(MediaFormat.KEY_LOW_LATENCY, 1)
                }
            }

            sMediaCodec?.configure(format, sSurface, null, 0)
            sMediaCodec?.start()
            sIsRunning = true
            Log.i(TAG, "MediaCodec initialized successfully for $mimeType")
            true
        } catch (e: IOException) {
            Log.e(TAG, "Failed to initialize MediaCodec: ${e.message}", e)
            false
        }
    }

    /**
     * Submits an encoded NAL unit byte buffer to MediaCodec.
     */
    @JvmStatic
    fun feedNalUnit(nalData: ByteArray, offset: Int, length: Int, presentationTimeUs: Long) {
        if (!sIsRunning || sMediaCodec == null) return
        try {
            val inputBufferIndex = sMediaCodec!!.dequeueInputBuffer(2000) // 2ms timeout
            if (inputBufferIndex >= 0) {
                val inputBuffer: ByteBuffer? = sMediaCodec!!.getInputBuffer(inputBufferIndex)
                if (inputBuffer != null) {
                    inputBuffer.clear()
                    inputBuffer.put(nalData, offset, length)
                    sMediaCodec!!.queueInputBuffer(inputBufferIndex, 0, length, presentationTimeUs, 0)
                }
            }
        } catch (e: Exception) {
            Log.w(TAG, "Error feeding NAL unit: ${e.message}")
        }
    }

    /**
     * Updates the surface texture with the latest decoded frame.
     */
    @JvmStatic
    fun updateTexImage() {
        try {
            sSurfaceTexture?.updateTexImage()
        } catch (ignored: Exception) {
        }
    }

    /**
     * Releases hardware resources.
     */
    @JvmStatic
    fun release() {
        sIsRunning = false
        try {
            sMediaCodec?.stop()
            sMediaCodec?.release()
        } catch (ignored: Exception) {}
        sMediaCodec = null

        try {
            sSurface?.release()
        } catch (ignored: Exception) {}
        sSurface = null

        try {
            sSurfaceTexture?.release()
        } catch (ignored: Exception) {}
        sSurfaceTexture = null
    }
}
