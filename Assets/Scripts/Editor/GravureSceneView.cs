using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Deckscent.Editor
{
    /// <summary>
    /// Le post-process gravure s'applique à toutes les caméras du renderer, vue Scène comprise.
    /// Dans la vue Scène, il gêne pour voir les assets (et l'obscurité avec la distance y met tout
    /// au noir) : on l'y coupe par défaut, la vue Game le garde. Menu Deckscent > Gravure dans la
    /// vue Scène pour le rallumer (réglage gardé d'une session à l'autre).
    /// </summary>
    [InitializeOnLoad]
    public static class GravureSceneView
    {
        const string MenuPath = "Deckscent/Gravure dans la vue Scène";
        const string PrefKey = "Deckscent.GravureDansVueScene";
        static readonly int GravureOff = Shader.PropertyToID("_GravureOff");

        static GravureSceneView()
        {
            RenderPipelineManager.beginCameraRendering += OnBeginCamera;
        }

        static bool ShowInSceneView => EditorPrefs.GetBool(PrefKey, false);

        // Chaque caméra fixe la valeur avant son rendu : la vue Scène et la vue Game alternent.
        static void OnBeginCamera(ScriptableRenderContext context, Camera camera)
        {
            var off = camera.cameraType == CameraType.SceneView && !ShowInSceneView;
            Shader.SetGlobalFloat(GravureOff, off ? 1f : 0f);
        }

        [MenuItem(MenuPath)]
        static void Toggle()
        {
            EditorPrefs.SetBool(PrefKey, !ShowInSceneView);
            SceneView.RepaintAll();
        }

        [MenuItem(MenuPath, true)]
        static bool ToggleValidate()
        {
            Menu.SetChecked(MenuPath, ShowInSceneView);
            return true;
        }
    }
}
