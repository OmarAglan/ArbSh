#include "shell.h"
#include <locale.h> // For setlocale()
#include <stdlib.h> // For getenv()
#include "platform/console.h" // Include console PAL

/* Define language codes */
#define LANG_EN 0
#define LANG_AR 1

/* Current language setting */
static int current_language = LANG_EN;

/* Message catalog for English */
static const char *messages_en[] = {
    "Welcome to ArbSh",                       /* MSG_WELCOME */
    "Command not found",                      /* MSG_CMD_NOT_FOUND */
    "Permission denied",                      /* MSG_PERMISSION_DENIED */
    "Cannot allocate memory",                 /* MSG_MEMORY_ERROR */
    "File not found",                         /* MSG_FILE_NOT_FOUND */
    "Invalid argument",                       /* MSG_INVALID_ARG */
    "Too many arguments",                     /* MSG_TOO_MANY_ARGS */
    "Not enough arguments",                   /* MSG_NOT_ENOUGH_ARGS */
    "Cannot open file",                       /* MSG_CANNOT_OPEN_FILE */
    "Cannot write to file",                   /* MSG_CANNOT_WRITE_FILE */
    "Type 'help' for more information",       /* MSG_HELP_HINT */
    "Exiting shell",                          /* MSG_EXIT */
    "History cleared",                        /* MSG_HISTORY_CLEARED */
    "Environment variable not found",         /* MSG_ENV_NOT_FOUND */
    "Environment variable set",               /* MSG_ENV_SET */
    "Environment variable unset",             /* MSG_ENV_UNSET */
    "Directory changed",                      /* MSG_DIR_CHANGED */
    "Cannot change directory",                /* MSG_CANNOT_CHANGE_DIR */
    "Alias created",                          /* MSG_ALIAS_CREATED */
    "Alias not found",                        /* MSG_ALIAS_NOT_FOUND */
    "Alias removed",                          /* MSG_ALIAS_REMOVED */
    "Command executed",                       /* MSG_CMD_EXECUTED */
    "Command failed",                         /* MSG_CMD_FAILED */
    "Syntax error",                           /* MSG_SYNTAX_ERROR */
    "$ "                                      /* MSG_PROMPT */
};

/* Message catalog for Arabic */
static const char *messages_ar[] = {
    "مرحبًا بك في ArbSh",                    /* MSG_WELCOME */
    "الأمر غير موجود",                       /* MSG_CMD_NOT_FOUND */
    "تم رفض الإذن",                          /* MSG_PERMISSION_DENIED */
    "لا يمكن تخصيص الذاكرة",                 /* MSG_MEMORY_ERROR */
    "الملف غير موجود",                       /* MSG_FILE_NOT_FOUND */
    "وسيطة غير صالحة",                       /* MSG_INVALID_ARG */
    "عدد كبير جدًا من الوسائط",              /* MSG_TOO_MANY_ARGS */
    "عدد غير كافٍ من الوسائط",               /* MSG_NOT_ENOUGH_ARGS */
    "لا يمكن فتح الملف",                     /* MSG_CANNOT_OPEN_FILE */
    "لا يمكن الكتابة إلى الملف",             /* MSG_CANNOT_WRITE_FILE */
    "اكتب 'help' لمزيد من المعلومات",        /* MSG_HELP_HINT */
    "الخروج من الصدفة",                      /* MSG_EXIT */
    "تم مسح السجل",                          /* MSG_HISTORY_CLEARED */
    "متغير البيئة غير موجود",                /* MSG_ENV_NOT_FOUND */
    "تم تعيين متغير البيئة",                 /* MSG_ENV_SET */
    "تم إلغاء تعيين متغير البيئة",           /* MSG_ENV_UNSET */
    "تم تغيير الدليل",                       /* MSG_DIR_CHANGED */
    "لا يمكن تغيير الدليل",                  /* MSG_CANNOT_CHANGE_DIR */
    "تم إنشاء الاسم المستعار",               /* MSG_ALIAS_CREATED */
    "الاسم المستعار غير موجود",              /* MSG_ALIAS_NOT_FOUND */
    "تمت إزالة الاسم المستعار",              /* MSG_ALIAS_REMOVED */
    "تم تنفيذ الأمر",                        /* MSG_CMD_EXECUTED */
    "فشل الأمر",                             /* MSG_CMD_FAILED */
    "خطأ في بناء الجملة",                    /* MSG_SYNTAX_ERROR */
    "$ "                                      /* MSG_PROMPT */
};

/**
 * set_language - Sets the current language
 * @lang_code: Language code (0 for English, 1 for Arabic)
 *
 * Return: 0 on success, -1 on failure
 */
int set_language(int lang_code)
{
    if (lang_code != LANG_EN && lang_code != LANG_AR)
        return -1;
    
    current_language = lang_code;
    
    /* Set text direction based on language */
    if (lang_code == LANG_AR) {
        /* Arabic is right-to-left */
        platform_console_set_text_direction(1);
    } else {
        /* English is left-to-right */
        platform_console_set_text_direction(0);
    }
    
    return 0;
}

/**
 * get_language - Gets the current language code
 *
 * Return: Current language code
 */
int get_language(void)
{
    return current_language;
}

/**
 * get_message - Gets a localized message
 * @msg_id: Message ID
 *
 * Return: Localized message string
 */
const char *get_message(int msg_id)
{
    if (msg_id < 0 || msg_id >= MSG_COUNT)
        return "Unknown message";
    
    if (current_language == LANG_AR)
        return messages_ar[msg_id];
    else
        return messages_en[msg_id];
}

/**
 * detect_system_language - Detects the system language
 *
 * Return: Language code based on system settings
 */
int detect_system_language(void)
{
    char *lang = getenv("LANG");
    
    if (lang && (strstr(lang, "ar") == lang || strstr(lang, "AR") == lang))
        return LANG_AR;
    
    return LANG_EN;
}

/**
 * init_locale - Initialize locale settings
 *
 * Return: 0 on success, -1 on failure
 */
int init_locale(void)
{
    int detected_lang;
    
    /* Set C library locale (important for functions like strftime, numeric formats) */
    setlocale(LC_ALL, "");

    /* Detect system language */
    detected_lang = detect_system_language();
    
    /* Set language (this will also set the text direction) */
    set_language(detected_lang);
    
    return 0;
} 