using System;
using UnityEngine;

public class WaitUntilOrTimeout : CustomYieldInstruction
{
    private readonly Func<bool> _condition;
    private readonly float _timeout;
    private readonly Action _onTimeout;

    public WaitUntilOrTimeout(Func<bool> condition, float timeout, Action onTimeout)
    {
        _condition = condition;
        _timeout = Time.time + timeout;
        _onTimeout = onTimeout;
    }

    public override bool keepWaiting
    {
        get
        {
            if (_condition()) return false;
            if (Time.time < _timeout) return true;
            _onTimeout();
            return false;

        }
    }

    public override string ToString()
    {
        return $"WaitUntilOrTimeout: {Time.time} < {_timeout}";
    }
}