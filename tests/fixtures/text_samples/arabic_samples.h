#ifndef ARABIC_SAMPLES_H
#define ARABIC_SAMPLES_H

/* Simple Arabic text samples for testing */

/* Basic Arabic greeting */
const char *ARABIC_GREETING = "مرحبا بالعالم";

/* Mixed Arabic and English */
const char *MIXED_TEXT = "هذا النص يحتوي English words في وسطه";

/* Arabic with numbers */
const char *ARABIC_WITH_NUMBERS = "العدد ١٢٣٤٥ والعدد 67890";

/* Arabic with diacritics */
const char *ARABIC_WITH_DIACRITICS = "العَرَبِيَّة مَعَ تَشْكِيل كَامِل";

/* Complex bidirectional text */
const char *COMPLEX_BIDI = "This is English text with العربية in the middle and more English";

/* Nested bidirectional text */
const char *NESTED_BIDI = "هذا نص عربي (with English (وعربي) inside) ونهاية عربية";

/* Arabic command example */
const char *ARABIC_COMMAND = "اطبع مرحبا";

/* RTL formatting characters */
const char *RTL_MARKERS = "\u200F\u202Eهذا نص\u202C\u200E";

/* Arabic punctuation and special characters */
const char *ARABIC_PUNCTUATION = "هل أنت جاهز؟ نعم، أنا جاهز!";

/* Very long Arabic text for stress testing */
const char *LONG_ARABIC_TEXT = 
    "هذا نص طويل باللغة العربية يستخدم لاختبار قدرة النظام على معالجة "
    "النصوص الطويلة. يجب أن يكون النظام قادرًا على التعامل مع هذا النص "
    "بشكل صحيح، بما في ذلك عرضه وتحريره والتنقل داخله. كما يجب أن يكون "
    "قادرًا على التعامل مع الأحرف الخاصة مثل (ء، أ، إ، آ، ة، ى) وكذلك "
    "علامات الترقيم مثل (،) و(؛) و(؟) و(!) وغيرها من العلامات الخاصة.";

#endif /* ARABIC_SAMPLES_H */ 