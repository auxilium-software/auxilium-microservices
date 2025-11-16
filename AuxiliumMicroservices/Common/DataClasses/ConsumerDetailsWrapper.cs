using System;
using System.Threading;
using System.Threading.Tasks;

namespace AuxiliumMicroservices.Common.DataClasses
{
    internal class ConsumerDetailsWrapper
    {
        private string consumerName;
        private readonly Func<CancellationToken, Task<bool>> _factory;
        private int successfulJobs = 0;
        private int failedJobs = 0;

        public ConsumerDetailsWrapper(string consumerName, Func<CancellationToken, Task<bool>> factory)
        {
            this.consumerName = consumerName;
            this._factory = factory;
        }

        public string GetConsumerName() => this.consumerName;
        public Func<CancellationToken, Task<bool>> GetFactory() => this._factory;
        public int GetSuccessfulJobs() => this.successfulJobs;
        public int GetFailedJobs() => this.failedJobs;
        public int GetTotalJobs() => this.successfulJobs + this.failedJobs;
        public void IncrementSuccessfulJobs() { this.successfulJobs++; }
        public void IncrementFailedJobs() { this.failedJobs++; }

        public Task<bool> StartTask(CancellationToken cancellationToken) => _factory(cancellationToken);
    }
}
