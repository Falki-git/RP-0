﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RP0.Crew
{
    public class ActiveCourse : CourseTemplate
    {
        public List<ProtoCrewMember> Students = new List<ProtoCrewMember>();

        public double elapsedTime = 0;
        public bool Started = false, Completed = false;


        public ActiveCourse(CourseTemplate template)
        {
            sourceNode = template.sourceNode;
            PopulateFromSourceNode(template.sourceNode);
        }

        public ActiveCourse(ConfigNode node)
        {
            FromConfigNode(node);
        }

        public ConfigNode AsConfigNode()
        {
            //save the source node, the variables, the teacher name, the student names, Started/Completed and the elapsed time
            ConfigNode node = new ConfigNode("ACTIVE_COURSE");
            node.AddValue("id", id);
            node.AddValue("name", name);
            node.AddValue("elapsedTime", elapsedTime);
            node.AddValue("Started", Started);
            node.AddValue("Completed", Completed);
            ConfigNode studentNode = new ConfigNode("STUDENTS");
            foreach (ProtoCrewMember student in Students)
                studentNode.AddValue("student", student.name);
            node.AddNode("STUDENTS", studentNode);

            node.AddNode("SOURCE_NODE", sourceNode);

            return node;
        }

        public void FromConfigNode(ConfigNode node)
        {
            node.TryGetValue("elapsedTime", ref elapsedTime);
            node.TryGetValue("Started", ref Started);
            node.TryGetValue("Completed", ref Completed);

            //load students
            ConfigNode studentNode = node.GetNode("STUDENTS");
            if (studentNode != null)
            {
                Students.Clear();
                foreach (ConfigNode.Value val in studentNode.values)
                {
                    if (HighLogic.CurrentGame.CrewRoster.Exists(val.value))
                    {
                        Students.Add(HighLogic.CurrentGame.CrewRoster[val.value]);
                    }
                }
            }

            sourceNode = node.GetNode("SOURCE_NODE");

            PopulateFromSourceNode(sourceNode);
        }

        public bool MeetsStudentReqs(ProtoCrewMember student)
        {
            return (student.type == (ProtoCrewMember.KerbalType.Crew) && (seatMax <= 0 || Students.Count < seatMax) && !student.inactive && student.rosterStatus == ProtoCrewMember.RosterStatus.Available && student.experienceLevel >= minLevel &&
                student.experienceLevel <= maxLevel && (classes.Length == 0 || classes.Contains(student.trait)) && !Students.Contains(student));
        }
        public void AddStudent(ProtoCrewMember student)
        {
            if (seatMax <= 0 || Students.Count < seatMax)
            {
                if (!Students.Contains(student))
                    Students.Add(student);
            }
        }
        public void AddStudent(string student)
        {
            AddStudent(HighLogic.CurrentGame.CrewRoster[student]);
        }
        
        public void RemoveStudent(ProtoCrewMember student)
        {
            if (Students.Contains(student))
            {
                Students.Remove(student);
                if (Started)
                {
                    UnityEngine.Debug.Log("[FS] Kerbal removed from in-progress class!");
                    //TODO: Assign partial rewards, based on what the REWARD nodes think
                }
            }
        }
        public void RemoveStudent(string student)
        {
            RemoveStudent(HighLogic.CurrentGame.CrewRoster[student]);
        }

        public bool ProgressTime(double dT)
        {
            if (!Started)
                return false;
            if (!Completed)
            { 
                elapsedTime += dT;
                Completed = elapsedTime >= time;
                if (Completed) //we finished the course!
                {
                    CompleteCourse();
                }
            }
            return Completed;
        }

        public void CompleteCourse()
        {

            //assign rewards to all kerbals and set them to free
            if (Completed)
            {
                foreach (ProtoCrewMember student in Students)
                {
                    if (student == null)
                        continue;

                    if (rewardXP != 0)
                        student.ExtraExperience += rewardXP;

                    if (RewardLog != null)
                    {
                        student.flightLog.AddFlight();
                        foreach (ConfigNode.Value v in RewardLog.values)
                        {
                            string[] s = v.value.Split(new char[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                            student.flightLog.AddEntry(s[0], s.Length == 1 ? null : s[1]);
                        }
                    }
                }
            }

            foreach (ProtoCrewMember student in Students)
                student.inactive = false;

            //fire an event
        }

        public bool StartCourse()
        {
            //set all the kerbals to unavailable and begin tracking time
            if (Started)
                return true;

            //ensure we have more than the minimum number of students and not more than the maximum number
            int studentCount = Students.Count;
            if (seatMax > 0 && studentCount > seatMax)
                return false;
            if (seatMin > 0 && studentCount < seatMin)
                return false;

            Started = true;

            foreach (ProtoCrewMember student in Students)
                student.SetInactive(time + 1d);

            return true;
            //fire an event
        }
    }
}
