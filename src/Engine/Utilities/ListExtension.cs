using System.Collections.Generic;

namespace MiniEngine.Utilities
{
    public static class ListExtension
    {
        /// <summary>
        /// Resizes the List<T> to a specified size. 
        /// If the new size is smaller, the list is truncated. 
        /// If larger, the list is padded with default values.
        /// </summary>
        public static void Resize<T>(this List<T> list, int newSize, T defaultValue = default)
        {
            int currentSize = list.Count;

            if (newSize < currentSize)
            {
                // Shrink: Remove elements from the end
                list.RemoveRange(newSize, currentSize - newSize);
            }
            else if (newSize > currentSize)
            {
                // Grow: Increase capacity if needed and add defaults
                if (newSize > list.Capacity)
                {
                    list.Capacity = newSize;
                }

                int countToAdd = newSize - currentSize;
                for (int i = 0; i < countToAdd; i++)
                {
                    list.Add(defaultValue);
                }
            }
        }
    }
}