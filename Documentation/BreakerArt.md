# Crystal Breaker — تطبیق با مرجع

مرجع کاربر در `Documentation/CrystalBreakerReference.png` و تصویر خروجی واقعی Unity در `Documentation/CrystalBreaker.png` ذخیره شده‌اند. تصویر خروجی از یک پرواز آزمایشی توپ در مرحلهٔ اول گرفته شده است؛ شمارهٔ مرحله و امتیاز در بازی از وضعیت واقعی آن خوانده می‌شوند.

## دارایی‌ها و چیدمان

- `Assets/Resources/Art/BreakerLandscape.png`: منظرهٔ مستقل بدون آجر، توپ، سکو، قاب یا نوشته؛ رندر روی سطح ۴۰×۲۵.
- `Assets/Resources/Art/BreakerSprites.png`: اطلس RGBA با ابعاد ۱۵۳۶×۱۰۲۴، شامل چهار آجر فیروزه‌ای/آبی/بنفش/طلایی، سکو، توپ، خرده‌شیشه و درخشش برخورد. آلفای دارایی حفظ شده و برش‌ها و محورهای هر شیء در `LoadBreakerSprites` تنظیم شده‌اند.
- `Assets/Shaders/BreakerLightTrail.shader`: رد نور واقعی TrailRenderer با محوشدن عرض و طول، برای توپ‌های متحرک.
- قاب نورانی و تزئینات لوزی با هندسهٔ خود Unity ساخته شده‌اند. اطلاعات بالای صفحه فونت serif نصب‌شدهٔ macOS را استفاده می‌کنند؛ فایل فونت سیستم در مخزن کپی نشده است.

زمین از نوار بالایی تا قاب پایین امتداد دارد. آجرها در چهار ردیف و هشت ستون با پنج جای خالی مطابق مرجع قرار گرفته‌اند. سکو و توپ در مختصات قوانین حرکت می‌کنند و تبدیل یکنواخت ۱٫۶۳ برای موقعیت رندر و تبدیل معکوس ورودی ماوس استفاده می‌شود. اندازهٔ برخورد سکو و آجرها با اندازهٔ تصویری هماهنگ است. بهترین امتیاز، راهنما و دکمه‌های خروج به هاب و شروع دوباره داخل منوی توقف هستند. Combo امتیاز واقعی دارد و با تماس سکو صفر می‌شود.

## تولید تصاویر

دارایی‌ها با ابزار داخلی `image_gen` و مهارت imagegen، با همان تصویر کاربر به‌عنوان edit target ساخته شدند. خروجی انتخاب‌شده از پوشهٔ generated_images به مسیرهای پروژهٔ بالا کپی شد. پرامپت‌های نهایی:

### منظره

> Use case: precise-object-edit. Edit target: attached Crystal Breaker game reference. Produce a clean background plate ONLY for this live Unity game. Remove ALL HUD lettering, score, title, lives, pause button, border/frame, diamonds, ALL bricks, ALL ball/trail/sparks, paddle and paddle reflection. Inpaint those areas seamlessly with the existing underlying landscape. Preserve exact mountain silhouette, horizon at 69 percent image height, deep navy starfield, sweeping cyan aurora, snowy mountains, shore and dark lake reflections, exact palette and panoramic composition. No game objects, no text, no border. Aspect 16:10, high resolution. Keep the reference landscape as identical as possible.

### اطلس کریستال‌ها

> Use case: background-extraction. Input image: source design for exact asset extraction. Generate a transparent game sprite sheet 1536x1024, a strict grid of 4 columns by 2 rows, 8 cells each 384x512. Each cell fully transparent outside its ONE isolated object. Extract/reproduce exact faceted polished rectangular beveled glass bricks seen in the reference, luminous thin white cyan rim, irregular overlapping translucent diamond/triangle facets, detailed light refracting interior, frontal orthographic view, NO perspective. Top row cells left to right: cyan brick, deep sapphire blue brick, violet brick, pale champagne gold brick. Each brick centered at the exact center of its cell, visible width 320 px and height 130 px, identical silhouette, generous transparent padding, no shadows behind bricks and no labels. Bottom row: cell 1 a long transparent cyan crystal PADDLE with angular pointed diamond end caps identical to the reference paddle, width 340 and height 40 centered; cell 2 one luminous cyan-white BALL circular pearl diameter 100 centered with soft cyan glow; cell 3 a small isolated sharp glass diamond crystal SHARD cyan, 70x110 centered; cell 4 a diffuse circular pale cyan IMPACT GLOW with white core and rays, diameter 220 centered. No lettering, NO checkerboard, no opaque backgrounds. Preserve source design crystal texture and realistic refraction.

خروجی اطلس دقیقاً شبکهٔ درخواستی را رعایت نکرد؛ برش‌های ردیف دوم و اندازه‌های رندر با آلفای واقعی آن کالیبره شدند. هیچ تصویر ثابت از آجرها یا سکو داخل منظره باقی نمانده است.

## اعتبارسنجی

۴۵ بررسی قوانین با `dotnet run --project Tools/RulesTests.csproj`؛ نتایج در `Documentation/RulesTests.txt`. ۵۴ بررسی اجرایی مک با `--smoke-test` شامل هاب، سه بازی، نمایش هدر، آجرهای مستقل، توقف و ادامه و قدرت‌ها و تغییر ظاهر پس از برخورد اول و شکستن پس از برخورد دوم است؛ نتایج در `Documentation/MacRuntimeTests.txt`. خروجی مک Universal برای arm64 و x86_64 است. خروجی وب و Android در این تغییر ساخته یا منتشر نشده‌اند.

## دوام آجرها

آجر معمولی همان قاب باریک قبلی را دارد. آجر دوضربه‌ای از مرحلهٔ سوم، قاب دولایه و چهار گوشهٔ الماسی دارد؛ ضربهٔ اول تصویرش را همان لحظه به قاب شکسته و ترک مرکزی تغییر می‌دهد و رنگ آن حفظ می‌شود. `MaxHP` دوام اولیه را نگه می‌دارد تا آجر مقاوم با یک ضربهٔ باقی‌مانده با آجر معمولی اشتباه نشود. ضربهٔ دوم آن را از بین می‌برد.

اطلس جدید RGBA: `Assets/Resources/Art/BreakerDurability.png`، ابعاد واقعی ۱۷۷۴×۸۸۷، چهار رنگ در دو حالت سالم و آسیب‌دیده. برش، محور و اندازهٔ نمایش از حدود آلفای واقعی کالیبره شده‌اند. تصویر واقعی مرحلهٔ سوم: `Documentation/CrystalBreakerDurability.png`.

تولید با `image_gen` در حالت edit، با طرح سه‌حالتهٔ تأییدشده به‌عنوان مرجع: `exec-a5700488-7519-4e01-b487-9b68476cb1da.png`. خروجی اصلی `exec-e573425d-0c84-40ef-ba39-f17cf86c11aa.png` در پوشهٔ generated_images نگه داشته شده است. پرامپت نهایی:

> Production game sprite atlas, edit/reference input is the approved Crystal Breaker durability design. Extract the CENTER armored pristine brick and RIGHT damaged armored brick into game-ready sprites. True transparent RGBA background, NO text, no labels, no backdrop, no cards. Strict 4 columns x 2 rows grid on a 2048x1024 canvas. Each cell exactly 512x512. Top row: intact ARMORED bricks colored cyan, sapphire blue, violet, champagne gold. Bottom row: corresponding DAMAGED armored bricks after one hit, same four colors. Every brick perfectly centered in its own cell, identical frontal orthographic angle, identical 400px-wide by 185px-high outer bounds including reinforced corner caps. Generous completely transparent padding. Keep approved realistic translucent crystal facets, WHITE luminous DOUBLE layered rim and four chunky diamond reinforcement corners for intact bricks. Damaged bricks keep the same connected rectangular main core but have clearly shattered outer frame, broken two corner caps, dark central impact star and branching luminous cracks, one remaining hit. Keep debris contained inside each cell close to the broken rim, no explosion. Preserve hue in each column between both states. Damage must be unmistakable in a small sprite. No cartoon or vector or opaque background.

## خرده‌شیشه، جایزه و تعویض مرحله

خرده‌شیشه‌ها از همان تصویر الماسی قبلی استفاده می‌کنند؛ اندازه، حرکت و مدت نمایش اصلی حفظ شده‌اند. شیدر `BreakerShard` فقط بخش رنگی فیروزه‌ای را به رنگ آجر تبدیل می‌کند و شکست نور سفید و آلفای اصلی را حفظ می‌کند. جایزه‌ها به شکل الماس طلایی با نماد سکوی عریض و الماس بنفش با سه گوی نمایش داده می‌شوند؛ رد نور و درخشش دارند و بدون متن، باکس یا مسدودکردن ورودی هستند.

شکستن آخرین آجر، بازهٔ ۲٫۴ ثانیه‌ای پایان مرحله را آغاز می‌کند: توپ‌ها و جایزه‌ها جمع می‌شوند، انیمیشن شکستن ادامه دارد، نشان کریستالی با عدد رومی مرحلهٔ بعد و NEXT CHAPTER ظاهر می‌شود و ذرات کریستالی از سکو به محل آجرها حرکت می‌کنند و حرکت قوانین متوقف است. سپس آجرهای مرحلهٔ بعد طی ۰٫۶۵ ثانیه ظاهر می‌شوند و توپ منتظر فرمان پرتاب کاربر می‌ماند. توقف بازی و خروج به هاب، زمان این بازه را نگه می‌دارد. تصویر اجرایی اثرها: `Documentation/CrystalBreakerEffects.png`؛ اجرای تشخیصی با `--breaker-effects-capture`.

## هدر و نمادهای تأییدشده

اطلس RGBA `Assets/Resources/Art/BreakerHud.png`، اندازهٔ واقعی ۱۲۵۴×۱۲۵۴، ۹ دارایی: لوگوی کریستالی، قاب خالی امتیاز، مدال خالی مرحله، کریستال جان، نماد توقف، دو جایزه، حلقهٔ تشکیل مرحله و درخشش. اعداد امتیاز و مرحله و تعداد جان‌ها از قوانین واقعی خوانده می‌شوند. برش‌ها با آلفای واقعی کالیبره شدند. توقف همچنان دکمهٔ قابل کلیک است.

مرجع تأییدشده `exec-17390463-2135-4d7c-ac6d-1659982110e9.png`؛ تولید edit با ابزار image_gen. خروجی اصلی `exec-5d42667b-acab-49ee-875d-b2c1a8653e79.png` در generated_images حفظ شده و به مسیر اطلس بالا کپی شد. پرامپت: Extract/reproduce the approved delicate crystalline game HUD and powerups as a production RGBA sprite atlas, transparent background, 3×3 centered isolated sprites, no dashboard boxes, thin faceted crystal lettering and rims; empty score/stage interiors for live counters; gold paddle with outward arrows, violet diamond with three luminous pearls; empty constellation seal; sharp life gem and diamond pause token.

تصاویر خروجی واقعی: `CrystalBreakerEffects.png` و `CrystalBreakerTransition.png`.
