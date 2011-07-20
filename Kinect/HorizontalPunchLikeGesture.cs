namespace Kinect
{
    using System;
    using System.Collections.Generic;
    using Microsoft.Research.Kinect.Nui;

    internal class HorizontalPunchLikeGesture : IGesture
    {
        private readonly float _treshold;
        private readonly JointID _joint;
        private LinkedList<PosInfo> _positions;
        private bool _previouslyOverTreshold;
        int _max = 0;

        private struct PosInfo
        {
            public float x;
            public DateTime when;
        }

        public HorizontalPunchLikeGesture(float treshold, JointID joint)
        {
            _treshold = treshold;
            _joint = joint;
            _positions = new LinkedList<PosInfo>();
            Duration = 150;
        }

        public HorizontalPunchLikeGesture(float treshold, JointID _joint, Action started)
            : this(treshold, _joint)
        {
            Started += o => started();
        }

        public HorizontalPunchLikeGesture(float treshold, JointID _joint, Action started, Action ended)
            : this(treshold, _joint)
        {
            Started += o => started();
            Ended += o => ended();
        }

        public event GestureEvent Ended;
        public event GestureEvent Started;

        private void OnEnded()
        {
            GestureEvent handler = Ended;
            if (handler != null) handler(this);
        }

        private void OnStarted()
        {
            GestureEvent handler = Started;
            if (handler != null) handler(this);
        }

        public void ProcessBodyFrame(Body body)
        {
            var now = DateTime.Now;
            _positions.AddLast(new LinkedListNode<PosInfo>(new PosInfo {x = body[_joint].X, when = now}));
            var overTreshold = OverTreshold(now);
            if (!_previouslyOverTreshold && overTreshold) OnStarted();
            else if (_previouslyOverTreshold && !overTreshold) OnEnded();
            _previouslyOverTreshold = overTreshold;
        }

        private bool OverTreshold(DateTime now)
        {
            if (_positions.Count > _max) _max = _positions.Count;
            while (_positions.First.Value.when < now - TimeSpan.FromMilliseconds(Duration))
            {
                _positions.RemoveFirst();
            }
            float span = 0;
            bool first = true;
            for (var iterator = _positions.First; iterator != null; iterator = iterator.Next)
            {
                if (first)
                {
                    first = false;
                    continue;
                }
                span += Math.Abs(iterator.Value.x - iterator.Previous.Value.x);
            }
            return span > _treshold;
        }

        protected double Duration { get; set; }
    }
}