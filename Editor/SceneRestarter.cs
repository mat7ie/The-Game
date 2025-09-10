using System.Collections;
using System.Collections.Generic;
using UnityEditor.Timeline.Actions;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UnityEditor
{
    public class SceneRestarter : MonoBehaviour
    {
        [MenuItem("Helpers/Restart Scene")]
        private static void RestartScene()
        {
            var currentScene = SceneManager.GetActiveScene();
            SceneManager.LoadScene(currentScene.name);
        }
    }
}
