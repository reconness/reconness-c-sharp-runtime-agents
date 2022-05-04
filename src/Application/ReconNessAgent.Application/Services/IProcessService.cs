﻿namespace ReconNessAgent.Application.Services
{
    public interface IProcessService
    {
        public Task ExecuteAsync(string agentInfoJson, CancellationToken cancellationToken = default);
    }
}
