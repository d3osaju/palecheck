using Microsoft.JSInterop;

namespace PaleCheck.Web.Services;

/// <summary>Lazily imports wwwroot/js/palecheck.js once and shares it.</summary>
public sealed class Js(IJSRuntime runtime) : IAsyncDisposable
{
    private Task<IJSObjectReference>? _module;

    public Task<IJSObjectReference> Module =>
        _module ??= runtime.InvokeAsync<IJSObjectReference>("import", "./js/palecheck.js").AsTask();

    public async ValueTask InvokeVoidAsync(string name, params object?[] args) =>
        await (await Module).InvokeVoidAsync(name, args);

    public async ValueTask<T> InvokeAsync<T>(string name, params object?[] args) =>
        await (await Module).InvokeAsync<T>(name, args);

    public async ValueTask DisposeAsync()
    {
        if (_module is { IsCompletedSuccessfully: true })
        {
            try { await _module.Result.DisposeAsync(); }
            catch (JSDisconnectedException) { }
        }
    }
}
