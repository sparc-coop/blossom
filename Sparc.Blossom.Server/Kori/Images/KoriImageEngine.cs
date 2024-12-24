using Microsoft.AspNetCore.Components.Forms;

namespace Sparc.Kori;

public class KoriImageEngine(KoriHttpEngine http, KoriJsEngine js)
{
    IBrowserFile? SelectedImage;


    public Task InitializeAsync()
    {
        return Task.CompletedTask;
    }

    public async Task BeginEditAsync()
    {
        await js.InvokeVoidAsync("editImage");
    }

    public void OnImageSelected(InputFileChangeEventArgs e)
    {
        SelectedImage = e.File;
    }

    public async Task BeginSaveAsync()
    {
        if (SelectedImage == null)
            return;

        var originalImageSrc = await js.InvokeAsync<string>("getActiveImageSrc");
        if (originalImageSrc != null)
        {
            var result = await http.SaveContentAsync(originalImageSrc, SelectedImage);
            if (result != null)
            {
                await js.InvokeVoidAsync("updateImageSrc", originalImageSrc, result.Text);
                Console.WriteLine("Image sent successfully!");
            }
            else
            {
                Console.WriteLine("Error sending image");
            }
        }
    }
}
