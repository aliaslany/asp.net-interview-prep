using System;
using System.Collections;
using System.Collections.Generic;

namespace InterviewPrep.CSharp
{
    // ================================================================
    //  Value Type در برابر Reference Type، و Boxing
    // ================================================================

    // struct = value type
    public struct PointStruct
    {
        public int X;
        public int Y;
    }

    // class = reference type
    public class PointClass
    {
        public int X;
        public int Y;
    }

    public class ValueVsReference
    {
        // ------------------------------------------------------------
        // تفاوت در کپی شدن
        // ------------------------------------------------------------
        public void CopyBehavior()
        {
            // --- Value Type: کپی مستقل ساخته می‌شود ---
            var s1 = new PointStruct { X = 1, Y = 1 };
            var s2 = s1;          // یک کپی کامل
            s2.X = 99;

            Console.WriteLine(s1.X);   // 1  ← دست‌نخورده
            Console.WriteLine(s2.X);   // 99

            // --- Reference Type: هر دو به یک آبجکت اشاره می‌کنند ---
            var c1 = new PointClass { X = 1, Y = 1 };
            var c2 = c1;          // فقط آدرس کپی شد
            c2.X = 99;

            Console.WriteLine(c1.X);   // 99 ← تغییر کرد!
            Console.WriteLine(c2.X);   // 99
        }

        // ------------------------------------------------------------
        // پاس دادن به متد
        // ------------------------------------------------------------
        public void PassingToMethods()
        {
            var s = new PointStruct { X = 1 };
            ModifyStruct(s);
            Console.WriteLine(s.X);    // 1 ← تغییر نکرد

            var c = new PointClass { X = 1 };
            ModifyClass(c);
            Console.WriteLine(c.X);    // 99 ← تغییر کرد

            // با ref حتی value type هم قابل تغییر است
            ModifyStructByRef(ref s);
            Console.WriteLine(s.X);    // 99
        }

        private void ModifyStruct(PointStruct p) => p.X = 99;
        private void ModifyClass(PointClass p) => p.X = 99;
        private void ModifyStructByRef(ref PointStruct p) => p.X = 99;

        // ------------------------------------------------------------
        // Boxing و Unboxing
        // ------------------------------------------------------------
        public void BoxingDemo()
        {
            int number = 42;

            // Boxing: عدد از stack کپی می‌شود در heap و در object بسته‌بندی می‌شود
            object boxed = number;          // ← تخصیص حافظه در heap

            // Unboxing: بازکردن بسته، نیاز به cast صریح دارد
            int unboxed = (int)boxed;

            // اگر نوع اشتباه cast کنی، در زمان اجرا خطا می‌دهد
            // long wrong = (long)boxed;    // InvalidCastException
        }

        // ------------------------------------------------------------
        // چرا Boxing بد است؟ مقایسه عملکرد
        // ------------------------------------------------------------
        public void WhyBoxingHurts()
        {
            // ArrayList از object استفاده می‌کند → هر افزودن یک boxing
            var oldList = new ArrayList();
            for (int i = 0; i < 1_000_000; i++)
                oldList.Add(i);            // ← یک میلیون بار boxing

            // List<T> جنریک است → اصلاً boxing ندارد
            var newList = new List<int>();
            for (int i = 0; i < 1_000_000; i++)
                newList.Add(i);            // ← بدون boxing
        }

        // ------------------------------------------------------------
        // Nullable — دادن قابلیت null به value type
        // ------------------------------------------------------------
        public void NullableDemo()
        {
            int? age = null;               // معادل Nullable<int>

            if (age.HasValue)
                Console.WriteLine(age.Value);

            // عملگر ?? برای مقدار پیش‌فرض
            int safeAge = age ?? 0;

            // عملگر ?. برای دسترسی امن
            PointClass p = null;
            int? x = p?.X;                 // به‌جای NullReferenceException، null می‌دهد
        }

        // ------------------------------------------------------------
        // برابری: == در برابر Equals
        // ------------------------------------------------------------
        public void EqualityDemo()
        {
            // رشته: هر دو محتوا را مقایسه می‌کنند (چون == در string سربارگذاری شده)
            string a = "hello";
            string b = "hel" + "lo";
            Console.WriteLine(a == b);          // True
            Console.WriteLine(a.Equals(b));     // True

            // آبجکت معمولی: == مرجع را مقایسه می‌کند
            var p1 = new PointClass { X = 1 };
            var p2 = new PointClass { X = 1 };
            Console.WriteLine(p1 == p2);        // False ← دو آبجکت جدا هستند
            Console.WriteLine(p1.Equals(p2));   // False ← چون Equals را override نکرده‌ایم

            // struct: Equals مقادیر را مقایسه می‌کند
            var s1 = new PointStruct { X = 1 };
            var s2 = new PointStruct { X = 1 };
            Console.WriteLine(s1.Equals(s2));   // True
        }
    }
}
