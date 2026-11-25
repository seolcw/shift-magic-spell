using System.IO;
using UnityEngine;
using UnityEngine.U2D;

namespace ShiftMagicSpell.Tests
{
    /// <summary>
    /// 배치 모드 테스트에서 화면을 PNG로 남긴다(눈으로 배치·연출을 확인하는 용도). 320x200으로 그린 뒤 3배로 키워 저장.
    /// 픽셀 퍼펙트 카메라는 잠깐 끄고 직접 렌더 텍스처에 그린다.
    /// </summary>
    public static class ScreenCaptureUtils
    {
        public const string OUTPUT_DIR = "Logs/Screens";
        private const int SCALE = 3;

        public static string Capture(string fileName)
        {
            return CaptureTo(OUTPUT_DIR, fileName, SCALE);
        }

        /// <summary>폴더와 배율을 정해 남긴다(영상 녹화는 배율 1로 빨리 찍고 나중에 키운다)</summary>
        public static string CaptureTo(string directory, string fileName, int scale = 1)
        {
            Camera camera = Camera.main;
            PixelPerfectCamera pixelPerfect = camera.GetComponent<PixelPerfectCamera>();
            bool wasEnabled = pixelPerfect != null && pixelPerfect.enabled;
            if (pixelPerfect != null)
            {
                pixelPerfect.enabled = false;
            }

            var target = new RenderTexture(320, 200, 24) { filterMode = FilterMode.Point };
            RenderTexture previousTarget = camera.targetTexture;
            float previousSize = camera.orthographicSize;
            camera.orthographicSize = 6.25f;
            camera.targetTexture = target;
            camera.Render();
            RenderTexture.active = target;
            var small = new Texture2D(320, 200, TextureFormat.RGB24, false);
            small.ReadPixels(new Rect(0, 0, 320, 200), 0, 0);
            small.Apply();
            RenderTexture.active = null;
            camera.targetTexture = previousTarget;
            camera.orthographicSize = previousSize;
            if (pixelPerfect != null)
            {
                pixelPerfect.enabled = wasEnabled;
            }

            Directory.CreateDirectory(directory);
            string path = Path.Combine(directory, fileName + ".png");
            if (scale <= 1)
            {
                File.WriteAllBytes(path, small.EncodeToPNG());
                Object.Destroy(target);
                Object.Destroy(small);
                return path;
            }

            var large = new Texture2D(320 * scale, 200 * scale, TextureFormat.RGB24, false);
            for (int y = 0; y < 200 * scale; y++)
            {
                for (int x = 0; x < 320 * scale; x++)
                {
                    large.SetPixel(x, y, small.GetPixel(x / scale, y / scale));
                }
            }

            File.WriteAllBytes(path, large.EncodeToPNG());
            Object.Destroy(target);
            Object.Destroy(small);
            Object.Destroy(large);
            return path;
        }
    }
}
