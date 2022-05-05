// COPYRIGHT 2021 by the Open Rails project.
//
// This file is part of Open Rails.
//
// Open Rails is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// Open Rails is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with Open Rails.  If not, see <http://www.gnu.org/licenses/>.

using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Orts.Parsers.Msts;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using JsonReader = Orts.Parsers.OR.JsonReader;

namespace Orts.Formats.OR
{
    /// <summary>
    /// Reads A.containers-or and parses it
    /// </summary>
    public class ContainersFile
    {
        /// <summary>
        /// Contains list of valid containers
        /// </summary>
        public List<Container> ContainerList = new List<Container>();
        public string Filename;

        private Container Container = null;
        private List<Container> UncheckedContainerList = new List<Container>();

        /// <summary>
        /// Reads JSON file, parsing valid data into containerShapeList and logging errors.
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="containerShapeList"></param>
        /// <param name="shapePath"></param>
        public ContainersFile(string filename)
        {
            Filename = filename; // Saved for use in error messages

            JsonReader.ReadFile(filename, TryParse);

            // Filter out objects with properties that are either invalid or missing.
            ContainerList = UncheckedContainerList
                // Checks for invalid names
                .Where(r => !string.IsNullOrEmpty(r.Name))
                .Where(r => !r.Name.Contains(" "))

                // Checks for missing fields
                .Where(r => r.Id != MissingField.MissingId)
                .Where(r => r.Name != MissingField.MissingName)
                .Where(r => r.Type != ContainerType.MissingType)
                .Where(r => r.Location != MissingField.MissingVector3)
                .ToList();
        }

        /// <summary>
        /// Parses next item from JSON data, populating a list of Containers and issuing warning messages.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        protected virtual bool TryParse(JsonReader item)
        {
            switch (item.Path)
            {
                case "":
                case "[]":
                    // Ignore these items. "[]" is found along the way to "[]."
                    break;

                case "[].":
                    Container = new Container();
                    UncheckedContainerList.Add(Container);
                    break;

                case "[].id":
                    Container.Id = item.AsInteger(-1);
                    break;

                case "[].name":
                    // Parse the property with default value as invalid, so errors can be detected and the object rejected later.
                    Container.Name = item.AsString("");

                    // Include any warning for invalid names here. For example:
                    if (string.IsNullOrWhiteSpace(Container.Name)
                    || Container.Name.Contains(" "))
                        Trace.TraceWarning($"Invalid name \"{Container.Name}\" referenced in {Filename}");
                    break;

                case "[].type":
                    Container.Type = item.AsEnum(ContainerType.Closed);
                    break;

                case "[].flipped":
                    Container.Flipped = item.AsBoolean(false);
                    break;

                case "[].location[]":
                    Container.Location = item.AsVector3(new Vector3(0, 0, 0));
                    break;

                default:
                    Trace.TraceWarning($"Unexpected entry \"{item.Path}\" found"); 
                    return false;
            }
            return true;
        }
    }

    /// <summary>
    /// Denotes a type of container.
    /// </summary>
    public enum ContainerType
    {
        MissingType,
        Open,
        Closed
    }

    public static class MissingField
    {
        public static string MissingName = "name missing>";
        public static Vector3 MissingVector3 = new Vector3(float.NaN, float.NaN, float.NaN);
        public static int MissingId = -1;
    }

    public class Container
    {
        public int Id { get; set; }
        public string Name { get; set; }

        [JsonConverter(typeof(StringEnumConverter))] // Provides automatic conversion parsing a string into an enum
        public ContainerType? Type { get; set; }

        public bool Flipped { get; set; }
        public Vector3 Location { get; set; }

        /// <summary>
        /// Give properties to a new Container which can be tested for missing entries
        /// </summary>
        public Container()
        {
            // Required properties get identifiable values
            Id = MissingField.MissingId;
            Name = MissingField.MissingName;
            Type = ContainerType.MissingType;
            Location = MissingField.MissingVector3;

            // Default properties
            Flipped = false;
        }
    }
}