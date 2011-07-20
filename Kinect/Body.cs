namespace Kinect
{
    using Microsoft.Research.Kinect.Nui;

    public class Body
    {
        public Vector Head { get; set; }

        public Vector this[JointID id]
        {
            get
            {
                switch (id)
                {
                    case JointID.Head:
                        return Head;
                    case JointID.HandLeft:
                        return LeftHand;
                    case JointID.HandRight:
                        return RightHand;
                }
                return new Vector();
            }
        }

        public Vector LeftHand { get; set; }
        public Vector RightHand { get; set; }

        public static Body FromSkeleton(SkeletonData skeleton)
        {
            return new Body
                       {
                           Head = skeleton.Joints[JointID.Head].Position,
                           RightHand = skeleton.Joints[JointID.HandRight].Position,
                           LeftHand = skeleton.Joints[JointID.HandLeft].Position,
                       };
        }
    }
}