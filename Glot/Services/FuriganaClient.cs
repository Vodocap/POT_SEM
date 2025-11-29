using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Glot.Services
{
    public class FuriganaClient
    {
        private readonly IJSRuntime _js;

        public FuriganaClient(IJSRuntime js)
        {
            _js = js;
        }

        public async Task<string?> ConvertToFuriganaAsync(string text)
        {
            try
            {
                return await _js.InvokeAsync<string>("convertToFurigana", text);
            }
            catch
            {
                return null;
            }
        }

        public async Task<string?> ConvertToRomajiAsync(string text)
        {
            try
            {
                return await _js.InvokeAsync<string>("convertToRomaji", text);
            }
            catch
            {
                return null;
            }
        }

        public async ValueTask<bool> IsReadyAsync()
        {
            try
            {
                return await _js.InvokeAsync<bool>("isKuroshiroReady");
            }
            catch
            {
                return false;
            }
        }

        public ValueTask ReloadAsync()
        {
            return _js.InvokeVoidAsync("reloadKuroshiro");
        }
    }
}
