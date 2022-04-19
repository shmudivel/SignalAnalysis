namespace SignalAnalysis;

/// <summary>
/// Load graphics resources from disk
/// </summary>
public class EmbeddedResources
{
	public const string AppLogo = @"images\logo.ico";

	/// <summary>
	/// Loads a graphics resource from a disk location
	/// </summary>
	/// <typeparam name="T">Type of resource to be loaded</typeparam>
	/// <param name="fileName">File name (absolute or relative to the working directory) to load resource from</param>
	/// <returns>The graphics resource</returns>
	public static T? LoadGraphicsResource<T> (string fileName)
    {
		T? resource = default;
		try
		{
			if (File.Exists(fileName))
			{
				if (typeof(T).Equals(typeof(System.Drawing.Image)))
					resource = (T)(object)Image.FromFile(fileName);
				else if (typeof(T).Equals(typeof(Icon)))
					resource = (T)(object)new Icon(fileName);
				else if (typeof(T).Equals(typeof(Cursor)))
					resource = (T)(object)new Cursor(fileName);
			}
		}
		catch (Exception ex)
		{
			MessageBox.Show(
				$"Unexpected error while loading the {fileName} graphics resource.{Environment.NewLine}{ex.Message}",
				"Loading error",
				MessageBoxButtons.OK,
				MessageBoxIcon.Error);
		}
		return resource;
	}
}
