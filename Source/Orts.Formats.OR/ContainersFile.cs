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
        public List<Container> Containers = new List<Container>();

        /// <summary>
        /// Reads JSON file, parsing valid data into containerShapeList and logging errors.
        /// </summary>
        /// <param name="filename"></param>
        public ContainersFile(string filename)
        {
            JsonReader.ReadFile(filename, TryParse);
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
                    if (item.TryRead(json => new Container(json), out var container))
                        Containers.Add(container);
                    break;
                default: return false;
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

    public class Container
    {
        public int Id = -1;
        public string Name = "";
        public ContainerType Type = ContainerType.MissingType;
        public bool Flipped;
        public Vector3 Location;

        /// <summary>
        /// Give properties to a new Container which can be tested for missing entries
        /// </summary>
        public Container(JsonReader json)
        {
            json.ReadBlock(TryParse);

            // These warnings apply to the entire object and will be reported on the "}"
            if (Id < 0) json.TraceWarning($"Skipped container missing property ID");
            if (Name.Length == 0) json.TraceWarning($"Skipped container missing property Name");
            if (Type == ContainerType.MissingType) json.TraceWarning($"Skipped container missing property Type");
            if (Location == Vector3.Zero) json.TraceWarning($"Skipped container missing property Location");
        }

        protected virtual bool TryParse(JsonReader item)
        {
            switch (item.Path)
            {
                case "id": Id = item.AsInteger(Id); break;
                case "name":
                    Name = item.AsString(Name);
                    // Include any warning for invalid names here. For example:
                    if (string.IsNullOrWhiteSpace(Name) || Name.Contains(" "))
                        item.TraceWarning($"Skipped container with invalid name \"{Name}\"");
                    break;
                case "type": Type = item.AsEnum(Type); break;
                case "flipped": Flipped = item.AsBoolean(Flipped); break;
                case "location[]": Location = item.AsVector3(Location); break;
                default: return false;
            }
            return true;
        }
    }
}