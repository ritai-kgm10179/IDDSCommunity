using System;
using System.Collections.Generic;

namespace IDDSCommunity.IntrusionDetection.Setup;

internal sealed class SetupRollbackJournal
{
    private readonly Stack<Action> rollbackActions = new();
    private bool committed;

    internal void Record(Action rollbackAction)
    {
        ArgumentNullException.ThrowIfNull(rollbackAction);
        if (committed) throw new InvalidOperationException(SetupText.Get("TransactionAlreadyCommitted"));
        rollbackActions.Push(rollbackAction);
    }

    internal void Commit()
    {
        committed = true;
        rollbackActions.Clear();
    }

    internal void RollBack()
    {
        if (committed) return;
        List<Exception>? failures = null;
        while (rollbackActions.TryPop(out Action? rollbackAction))
        {
            try
            {
                rollbackAction();
            }
            catch (Exception exception)
            {
                (failures ??= []).Add(exception);
            }
        }
        if (failures is not null) throw new AggregateException(failures);
    }
}
