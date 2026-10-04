# بررسی نسخهٔ ساخته‌شده

تاریخ بررسی: ۴ اکتبر ۲۰۲۶

Unity 6000.0.83f1 با URP 17.0.4، روی مک Apple M3 Pro با Metal اجرا شد. خروجی شامل هر دو معماری arm64 و x86_64 است؛ اجرای عملی روی Apple Silicon بررسی شد و اجرای Intel به‌صورت جدا آزمایش نشده است.

۱۳ آزمون قواعد بازی در .NET 10 موفق بودند. گزارش: `RulesTests.txt`.

۱۴ بررسی داخل برنامهٔ مستقل موفق بودند: بسته‌شدن منوی آغاز، ذخیرهٔ قطعه، رندر قطعهٔ ذخیره‌شده، حرکت و چرخش، امتیاز سقوط، آغاز و پایان انیمیشن پاک‌شدن خطوط، منوی توقف، ادامه، راهنما و بازگشت از آن، صدا، پایان بازی و شروع دوباره. گزارش: `SmokeTest.log`.

کنترل‌های صفحه‌کلید برای شروع، ذخیره، حرکت، چرخش، سقوط فوری و توقف نیز با رابط واقعی برنامه امتحان شدند.

`Gameplay.png` تصویر گرفته‌شده از رندر برنامهٔ مستقل است. برای بررسی ظاهر، چیدمان بلوک‌ها در این تصویر با حالت تشخیصی ساخته شده است؛ این تصویر خروجی imagegen نیست.

دستور ثبت تصویر:

```sh
Builds/iTetris.app/Contents/MacOS/iTetris --visual-test --capture-path "$PWD/Documentation/Gameplay.png" --quit-after-capture -logFile "$PWD/Documentation/Player.log"
```

دستور بررسی برنامهٔ مستقل:

```sh
Builds/iTetris.app/Contents/MacOS/iTetris --smoke-test -logFile "$PWD/Documentation/SmokeTest.log"
```

گزارش ساخت در `Build.log` با `ITETRIS_BUILD_SUCCEEDED` ثبت شده است. در بررسی نهایی، خطای کامپایل C#، خطای شیدر یا استثنای اجرایی دیده نشد.
