namespace Valhalla.MessageQueue.Nats;

internal class CancellationTokenDisposable : IDisposable
{
	private readonly CancellationTokenSource m_Cts;

	public CancellationToken Token => m_Cts.Token;

	public CancellationTokenDisposable(CancellationToken token = default)
	{
		m_Cts = CancellationTokenSource.CreateLinkedTokenSource(token);
	}

	public void Dispose()
	{
		if (!m_Cts.IsCancellationRequested)
		{
			m_Cts.Cancel();
		}
	}
}
