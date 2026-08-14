using UnityEditor;

public static class Build
{
    public static void BuildAndroid()
    {
        BuildPlayerOptions options =
            new BuildPlayerOptions();

        options.scenes =
            new[]
            {
                "Assets/Scenes/Main.unity"
            };

        options.locationPathName =
            "build/Island100.apk";

        options.target =
            BuildTarget.Android;

        options.options =
            BuildOptions.None;

        BuildPipeline.BuildPlayer(
            options
        );
    }
}
