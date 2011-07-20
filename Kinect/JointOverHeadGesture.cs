namespace Kinect
{
    using System;
    using Microsoft.Research.Kinect.Nui;

    internal class JointOverHeadGesture : IGesture
    {
        private readonly JointID _joint;
        private DateTime _lastChange;
        private bool _previouslyOver;

        public JointOverHeadGesture(JointID joint)
        {
            _joint = joint;
            _previouslyOver = false;
            ChangeTreshold = TimeSpan.FromMilliseconds(500);
        }

        public JointOverHeadGesture(JointID _joint, Action started)
            : this(_joint)
        {
            Started += o => started();
        }

        public JointOverHeadGesture(JointID _joint, Action started, Action ended)
            : this(_joint)
        {
            Started += o => started();
            Ended += o => ended();
        }

        public TimeSpan ChangeTreshold { get; set; }

        public event GestureEvent Ended;
        public event GestureEvent Started;

        private bool HandOverHead(Body body)
        {
            return body[_joint].Y > body[JointID.Head].Y;
        }

        private void OnEnded()
        {
            _lastChange = DateTime.Now;
            GestureEvent handler = Ended;
            if (handler != null) handler(this);
        }

        private void OnStarted()
        {
            _lastChange = DateTime.Now;
            GestureEvent handler = Started;
            if (handler != null) handler(this);
        }

        public void ProcessBodyFrame(Body body)
        {
            TimeSpan sinceLastChange = DateTime.Now - _lastChange;
            if (sinceLastChange < ChangeTreshold) return;
            bool handOverHead = HandOverHead(body);
            if (_previouslyOver && !handOverHead) OnEnded();
            else if (!_previouslyOver && handOverHead) OnStarted();
            _previouslyOver = handOverHead;
        }
    }
}