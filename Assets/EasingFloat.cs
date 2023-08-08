using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EasingFloat
{
    float _startTime = 0;
    float _currentTime = 0;
    float _easeTime = 0;
    bool _isReverse = false;

    public float StartTime {get=>_startTime;}
    public float EaseTime {get=>_easeTime;}
    public float Current {get=>_currentTime;set=>_currentTime = value;}

    public bool Complete {get=>(!_isReverse && _currentTime >= _easeTime)||(_isReverse && _currentTime <= _easeTime);}

    public static implicit operator float(EasingFloat e) => e._currentTime;

    public EasingFloat()
    {
    }

    public void Start(float easeTime, float startTime = 0)
    {
        _easeTime = easeTime;
        _startTime = startTime;
        _currentTime = startTime;
        _isReverse = _startTime > _easeTime;
    }

    public void Update(float deltaTime)
    {
        if (_isReverse)
            _currentTime = Mathf.Max(_easeTime, _currentTime - deltaTime);
        else
            _currentTime = Mathf.Min(_easeTime, _currentTime + deltaTime);
    }
}

public class EasingTwoFloats
{
    EasingFloat _floatX;
    EasingFloat _floatY;

    public EasingFloat x {get=>_floatX;}
    public EasingFloat y {get=>_floatY;}

    public EasingTwoFloats()
    {
        _floatX = new EasingFloat();
        _floatY = new EasingFloat();
    }
}

public class EasingThreeFloats
{
    EasingFloat _floatX;
    EasingFloat _floatY;
    EasingFloat _floatZ;

    public EasingFloat x {get=>_floatX;}
    public EasingFloat y {get=>_floatY;}
    public EasingFloat z {get=>_floatZ;}

    public EasingThreeFloats()
    {
        _floatX = new EasingFloat();
        _floatY = new EasingFloat();
        _floatZ = new EasingFloat();
    }
}