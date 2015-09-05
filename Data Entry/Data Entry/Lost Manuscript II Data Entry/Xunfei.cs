using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace Dialogue_Data_Entry
{
    public enum SampleStatus
    {
        MSP_AUDIO_SAMPLE_INIT = 0x00,
        MSP_AUDIO_SAMPLE_FIRST = 0x01,
        MSP_AUDIO_SAMPLE_CONTINUE = 0x02,
        MSP_AUDIO_SAMPLE_LAST = 0x04,
    }
    public enum RecognitionStatus
    {
        MSP_REC_NULL = -1,
        MSP_REC_STATUS_SUCCESS = 0,
        MSP_REC_STATUS_NO_MATCH = 1,
        MSP_REC_STATUS_INCOMPLETE = 2,
        MSP_REC_STATUS_NON_SPEECH_DETECTED = 3,
        MSP_REC_STATUS_SPEECH_DETECTED = 4,
        MSP_REC_STATUS_COMPLETE = 5,
        MSP_REC_STATUS_MAX_CPU_TIME = 6,
        MSP_REC_STATUS_MAX_SPEECH = 7,
        MSP_REC_STATUS_STOPPED = 8,
        MSP_REC_STATUS_REJECTED = 9,
        MSP_REC_STATUS_NO_SPEECH_FOUND = 10,
        MSP_REC_STATUS_FAILURE = MSP_REC_STATUS_NO_MATCH,
    }
    public enum EndpointStatus
    {
        MSP_EP_NULL = -1,
        MSP_EP_LOOKING_FOR_SPEECH = 0,
        MSP_EP_IN_SPEECH = 1,
        MSP_EP_AFTER_SPEECH = 3,
        MSP_EP_TIMEOUT = 4,
        MSP_EP_ERROR = 5,
        MSP_EP_MAX_SPEECH = 6,
        MSP_EP_IDLE = 7  // internal state after stop and before start
    }
    public enum SynthesizingStatus
    {
        MSP_TTS_FLAG_STILL_HAVE_DATA = 1,
        MSP_TTS_FLAG_DATA_END = 2,
        MSP_TTS_FLAG_CMD_CANCELED = 4,
    }
    public enum VoiceName
    {
        //intp65引擎：
        xiaoyan,//（青年女声，普通话）
        xiaoyu,//（青年男声，普通话）

        //intp65_en引擎:
        Catherine,//（英文女声）
        henry,//（英文男声）

        //vivi21引擎：
        vixy,//（小燕，普通话）
        vixm,//（小梅，粤语）
        vixl,//（小莉，台湾普通话）
        vixr,//（小蓉，四川话）
        vixyun,//（小芸，东北话）
    }
    public enum ErrorCode
    {
        MSP_SUCCESS = 0,
        MSP_ERROR_FAIL = -1,
        MSP_ERROR_EXCEPTION = -2,

        /* General errors 10100(0x2774) */
        MSP_ERROR_GENERAL = 10100,  /* 0x2774 */
        MSP_ERROR_OUT_OF_MEMORY = 10101,    /* 0x2775 */
        MSP_ERROR_FILE_NOT_FOUND = 10102,   /* 0x2776 */
        MSP_ERROR_NOT_SUPPORT = 10103,  /* 0x2777 */
        MSP_ERROR_NOT_IMPLEMENT = 10104,    /* 0x2778 */
        MSP_ERROR_ACCESS = 10105,   /* 0x2779 */
        MSP_ERROR_INVALID_PARA = 10106,     /* 0x277A */
        MSP_ERROR_INVALID_PARA_VALUE = 10107,   /* 0x277B */
        MSP_ERROR_INVALID_HANDLE = 10108,   /* 0x277C */
        MSP_ERROR_INVALID_DATA = 10109,     /* 0x277D */
        MSP_ERROR_NO_LICENSE = 10110,   /* 0x277E */
        MSP_ERROR_NOT_INIT = 10111,     /* 0x277F */
        MSP_ERROR_NULL_HANDLE = 10112,  /* 0x2780 */
        MSP_ERROR_OVERFLOW = 10113,     /* 0x2781 */
        MSP_ERROR_TIME_OUT = 10114,     /* 0x2782 */
        MSP_ERROR_OPEN_FILE = 10115,    /* 0x2783 */
        MSP_ERROR_NOT_FOUND = 10116,    /* 0x2784 */
        MSP_ERROR_NO_ENOUGH_BUFFER = 10117,     /* 0x2785 */
        MSP_ERROR_NO_DATA = 10118,  /* 0x2786 */
        MSP_ERROR_NO_MORE_DATA = 10119,     /* 0x2787 */
        MSP_ERROR_NO_RESPONSE_DATA = 10120,     /* 0x2788 */
        MSP_ERROR_ALREADY_EXIST = 10121,    /* 0x2789 */
        MSP_ERROR_LOAD_MODULE = 10122,  /* 0x278A */
        MSP_ERROR_BUSY = 10123,     /* 0x278B */
        MSP_ERROR_INVALID_CONFIG = 10124,   /* 0x278C */
        MSP_ERROR_VERSION_CHECK = 10125,    /* 0x278D */
        MSP_ERROR_CANCELED = 10126,     /* 0x278E */
        MSP_ERROR_INVALID_MEDIA_TYPE = 10127,   /* 0x278F */
        MSP_ERROR_CONFIG_INITIALIZE = 10128,    /* 0x2790 */
        MSP_ERROR_CREATE_HANDLE = 10129,    /* 0x2791 */
        MSP_ERROR_CODING_LIB_NOT_LOAD = 10130,  /* 0x2792 */
        MSP_ERROR_USER_CANCELLED = 10131,   /* 0x2793 */
        MSP_ERROR_INVALID_OPERATION = 10132,    /* 0x2794 */
        MSP_ERROR_MESSAGE_NOT_COMPLETE = 10133, /* 0x2795 */  //flash

        /* Error codes of network 10200(0x27D8)*/
        MSP_ERROR_NET_GENERAL = 10200,  /* 0x27D8 */
        MSP_ERROR_NET_OPENSOCK = 10201,     /* 0x27D9 */   /* Open socket */
        MSP_ERROR_NET_CONNECTSOCK = 10202,  /* 0x27DA */   /* Connect socket */
        MSP_ERROR_NET_ACCEPTSOCK = 10203,   /* 0x27DB */   /* Accept socket */
        MSP_ERROR_NET_SENDSOCK = 10204,     /* 0x27DC */   /* Send socket data */
        MSP_ERROR_NET_RECVSOCK = 10205,     /* 0x27DD */   /* Recv socket data */
        MSP_ERROR_NET_INVALIDSOCK = 10206,  /* 0x27DE */   /* Invalid socket handle */
        MSP_ERROR_NET_BADADDRESS = 10207,   /* 0x27EF */   /* Bad network address */
        MSP_ERROR_NET_BINDSEQUENCE = 10208,     /* 0x27E0 */   /* Bind after listen/connect */
        MSP_ERROR_NET_NOTOPENSOCK = 10209,  /* 0x27E1 */   /* Socket is not opened */
        MSP_ERROR_NET_NOTBIND = 10210,  /* 0x27E2 */   /* Socket is not bind to an address */
        MSP_ERROR_NET_NOTLISTEN = 10211,    /* 0x27E3 */   /* Socket is not listening */
        MSP_ERROR_NET_CONNECTCLOSE = 10212,     /* 0x27E4 */   /* The other side of connection is closed */
        MSP_ERROR_NET_NOTDGRAMSOCK = 10213,     /* 0x27E5 */   /* The socket is not datagram type */
        MSP_ERROR_NET_DNS = 10214,  /* 0x27E6 */   /* domain name is invalid or dns server does not function well */
        MSP_ERROR_NET_INIT = 10215,     /* 0x27E7 */   /* ssl ctx create failed */

        /* Error codes of mssp message 10300(0x283C) */
        MSP_ERROR_MSG_GENERAL = 10300,  /* 0x283C */
        MSP_ERROR_MSG_PARSE_ERROR = 10301,  /* 0x283D */
        MSP_ERROR_MSG_BUILD_ERROR = 10302,  /* 0x283E */
        MSP_ERROR_MSG_PARAM_ERROR = 10303,  /* 0x283F */
        MSP_ERROR_MSG_CONTENT_EMPTY = 10304,    /* 0x2840 */
        MSP_ERROR_MSG_INVALID_CONTENT_TYPE = 10305,     /* 0x2841 */
        MSP_ERROR_MSG_INVALID_CONTENT_LENGTH = 10306,   /* 0x2842 */
        MSP_ERROR_MSG_INVALID_CONTENT_ENCODE = 10307,   /* 0x2843 */
        MSP_ERROR_MSG_INVALID_KEY = 10308,  /* 0x2844 */
        MSP_ERROR_MSG_KEY_EMPTY = 10309,    /* 0x2845 */
        MSP_ERROR_MSG_SESSION_ID_EMPTY = 10310,     /* 0x2846 */
        MSP_ERROR_MSG_LOGIN_ID_EMPTY = 10311,   /* 0x2847 */
        MSP_ERROR_MSG_SYNC_ID_EMPTY = 10312,    /* 0x2848 */
        MSP_ERROR_MSG_APP_ID_EMPTY = 10313,     /* 0x2849 */
        MSP_ERROR_MSG_EXTERN_ID_EMPTY = 10314,  /* 0x284A */
        MSP_ERROR_MSG_INVALID_CMD = 10315,  /* 0x284B */
        MSP_ERROR_MSG_INVALID_SUBJECT = 10316,  /* 0x284C */
        MSP_ERROR_MSG_INVALID_VERSION = 10317,  /* 0x284D */
        MSP_ERROR_MSG_NO_CMD = 10318,   /* 0x284E */
        MSP_ERROR_MSG_NO_SUBJECT = 10319,   /* 0x284F */
        MSP_ERROR_MSG_NO_VERSION = 10320,   /* 0x2850 */
        MSP_ERROR_MSG_MSSP_EMPTY = 10321,   /* 0x2851 */
        MSP_ERROR_MSG_NEW_RESPONSE = 10322,     /* 0x2852 */
        MSP_ERROR_MSG_NEW_CONTENT = 10323,  /* 0x2853 */
        MSP_ERROR_MSG_INVALID_SESSION_ID = 10324,   /* 0x2854 */
        MSP_ERROR_MSG_INVALID_CONTENT = 10325,  /* 0x2855 */

        /* Error codes of DataBase 10400(0x28A0)*/
        MSP_ERROR_DB_GENERAL = 10400,   /* 0x28A0 */
        MSP_ERROR_DB_EXCEPTION = 10401,     /* 0x28A1 */
        MSP_ERROR_DB_NO_RESULT = 10402,     /* 0x28A2 */
        MSP_ERROR_DB_INVALID_USER = 10403,  /* 0x28A3 */
        MSP_ERROR_DB_INVALID_PWD = 10404,   /* 0x28A4 */
        MSP_ERROR_DB_CONNECT = 10405,   /* 0x28A5 */
        MSP_ERROR_DB_INVALID_SQL = 10406,   /* 0x28A6 */
        MSP_ERROR_DB_INVALID_APPID = 10407, /* 0x28A7 */

        /* Error codes of Resource 10500(0x2904)*/
        MSP_ERROR_RES_GENERAL = 10500,  /* 0x2904 */
        MSP_ERROR_RES_LOAD = 10501,     /* 0x2905 */   /* Load resource */
        MSP_ERROR_RES_FREE = 10502,     /* 0x2906 */   /* Free resource */
        MSP_ERROR_RES_MISSING = 10503,  /* 0x2907 */   /* Resource File Missing */
        MSP_ERROR_RES_INVALID_NAME = 10504,     /* 0x2908 */   /* Invalid resource file name */
        MSP_ERROR_RES_INVALID_ID = 10505,   /* 0x2909 */   /* Invalid resource ID */
        MSP_ERROR_RES_INVALID_IMG = 10506,  /* 0x290A */   /* Invalid resource image pointer */
        MSP_ERROR_RES_WRITE = 10507,    /* 0x290B */   /* Write read-only resource */
        MSP_ERROR_RES_LEAK = 10508,     /* 0x290C */   /* Resource leak out */
        MSP_ERROR_RES_HEAD = 10509,     /* 0x290D */   /* Resource head currupt */
        MSP_ERROR_RES_DATA = 10510,     /* 0x290E */   /* Resource data currupt */
        MSP_ERROR_RES_SKIP = 10511,     /* 0x290F */   /* Resource file skipped */

        /* Error codes of TTS 10600(0x2968)*/
        MSP_ERROR_TTS_GENERAL = 10600,  /* 0x2968 */
        MSP_ERROR_TTS_TEXTEND = 10601,  /* 0x2969 */  /* Meet text end */
        MSP_ERROR_TTS_TEXT_EMPTY = 10602,   /* 0x296A */  /* no synth text */
        MSP_ERROR_TTS_LTTS_ERROR = 10603,   /* 0x296B */

        /* Error codes of Recognizer 10700(0x29CC) */
        MSP_ERROR_REC_GENERAL = 10700,  /* 0x29CC */
        MSP_ERROR_REC_INACTIVE = 10701,     /* 0x29CD */
        MSP_ERROR_REC_GRAMMAR_ERROR = 10702,    /* 0x29CE */
        MSP_ERROR_REC_NO_ACTIVE_GRAMMARS = 10703,   /* 0x29CF */
        MSP_ERROR_REC_DUPLICATE_GRAMMAR = 10704,    /* 0x29D0 */
        MSP_ERROR_REC_INVALID_MEDIA_TYPE = 10705,   /* 0x29D1 */
        MSP_ERROR_REC_INVALID_LANGUAGE = 10706,     /* 0x29D2 */
        MSP_ERROR_REC_URI_NOT_FOUND = 10707,    /* 0x29D3 */
        MSP_ERROR_REC_URI_TIMEOUT = 10708,  /* 0x29D4 */
        MSP_ERROR_REC_URI_FETCH_ERROR = 10709,  /* 0x29D5 */

        /* Error codes of Speech Detector 10800(0x2A30) */
        MSP_ERROR_EP_GENERAL = 10800,   /* 0x2A30 */
        MSP_ERROR_EP_NO_SESSION_NAME = 10801,   /* 0x2A31 */
        MSP_ERROR_EP_INACTIVE = 10802,  /* 0x2A32 */
        MSP_ERROR_EP_INITIALIZED = 10803,   /* 0x2A33 */

        /* Error codes of TUV */
        MSP_ERROR_TUV_GENERAL = 10900,  /* 0x2A94 */
        MSP_ERROR_TUV_GETHIDPARAM = 10901,  /* 0x2A95 */   /* Get Busin Param huanid*/
        MSP_ERROR_TUV_TOKEN = 10902,    /* 0x2A96 */   /* Get Token */
        MSP_ERROR_TUV_CFGFILE = 10903,  /* 0x2A97 */   /* Open cfg file */
        MSP_ERROR_TUV_RECV_CONTENT = 10904,     /* 0x2A98 */   /* received content is error */
        MSP_ERROR_TUV_VERFAIL = 10905,  /* 0x2A99 */   /* Verify failure */

        /* Error codes of IMTV */
        MSP_ERROR_LOGIN_SUCCESS = 11000,    /* 0x2AF8 */   /* ³É¹¦ */
        MSP_ERROR_LOGIN_NO_LICENSE = 11001,     /* 0x2AF9 */   /* ÊÔÓÃ´ÎÊý½áÊø£¬ÓÃ»§ÐèÒª¸¶·Ñ */
        MSP_ERROR_LOGIN_SESSIONID_INVALID = 11002,  /* 0x2AFA */   /* SessionIdÊ§Ð§£¬ÐèÒªÖØÐÂµÇÂ¼Í¨ÐÐÖ¤ */
        MSP_ERROR_LOGIN_SESSIONID_ERROR = 11003,    /* 0x2AFB */   /* SessionIdÎª¿Õ£¬»òÕß·Ç·¨ */
        MSP_ERROR_LOGIN_UNLOGIN = 11004,    /* 0x2AFC */   /* Î´µÇÂ¼Í¨ÐÐÖ¤ */
        MSP_ERROR_LOGIN_INVALID_USER = 11005,   /* 0x2AFD */   /* ÓÃ»§IDÎÞÐ§ */
        MSP_ERROR_LOGIN_INVALID_PWD = 11006,    /* 0x2AFE */   /* ÓÃ»§ÃÜÂëÎÞÐ§ */
        MSP_ERROR_LOGIN_SYSTEM_ERROR = 11099,   /* 0x2B5B */   /* ÏµÍ³´íÎó */

        /* Error Codes using in local engine */
        MSP_ERROR_AUTH_NO_LICENSE = 11200,  /* 0x2BC0 */   /* ÎÞÊÚÈ¨ */
        MSP_ERROR_AUTH_NO_ENOUGH_LICENSE = 11201,   /* 0x2BC1 */   /* ÊÚÈ¨²»×ã */
        MSP_ERROR_AUTH_INVALID_LICENSE = 11202, /* 0x2BC2 */   /* ÎÞÐ§µÄÊÚÈ¨ */
        MSP_ERROR_AUTH_LICENSE_EXPIRED = 11203, /* 0x2BC3 */   /* ÊÚÈ¨¹ýÆÚ */
        MSP_ERROR_AUTH_NEED_MORE_DATA = 11204,    /* 0x2BC4 */   /* ÎÞÉè±¸ÐÅÏ¢ */
        MSP_ERROR_AUTH_LICENSE_TO_BE_EXPIRED = 11205,   /* 0x2BC5 */   /* ÊÚÈ¨¼´½«¹ýÆÚ£¬¾¯¸æÐÔ´íÎóÂë */

        /* Error codes of HCR */
        MSP_ERROR_HCR_GENERAL = 11100,
        MSP_ERROR_HCR_RESOURCE_NOT_EXIST = 11101,
        MSP_ERROR_HCR_CREATE = 11102,
        MSP_ERROR_HCR_DESTROY = 11103,
        MSP_ERROR_HCR_START = 11104,
        MSP_ERROR_HCR_APPEND_STROKES = 11105,
        MSP_ERROR_HCR_INIT = 11106,
        MSP_ERROR_HCR_POINT_DECODE = 11107,
        MSP_ERROR_HCR_DISPATCH = 11108,
        MSP_ERROR_HCR_GETRESULT = 11109,
        MSP_ERROR_HCR_RESOURCE = 11110,




        /* Error codes of http 12000(0x2EE0) */
        MSP_ERROR_HTTP_BASE = 12000,    /* 0x2EE0 */

        /*Error codes of ISV */
        MSP_ERROR_ISV_NO_USER = 13000,  /* 32C8 */    /* the user doesn't exist */

        /* Error codes of Lua scripts */
        MSP_ERROR_LUA_BASE = 14000,    /* 0x36B0 */
        MSP_ERROR_LUA_YIELD = 14001,    /* 0x36B1 */
        MSP_ERROR_LUA_ERRRUN = 14002,   /* 0x36B2 */
        MSP_ERROR_LUA_ERRSYNTAX = 14003,    /* 0x36B3 */
        MSP_ERROR_LUA_ERRMEM = 14004,   /* 0x36B4 */
        MSP_ERROR_LUA_ERRERR = 14005,   /* 0x36B5 */
        MSP_ERROR_LUA_INVALID_PARAM = 14006,    /* 0x36B6 */

        /* Error codes of MMP */
        MSP_ERROR_MMP_BASE = 15000,    /* 0x3A98 */
        MSP_ERROR_MMP_MYSQL_INITFAIL = 15001,   /* 0x3A99 */
        MSP_ERROR_MMP_REDIS_INITFAIL = 15002,   /* 0x3A9A */
        MSP_ERROR_MMP_NETDSS_INITFAIL = 15003,  /* 0x3A9B */
        MSP_ERROR_MMP_TAIR_INITFAIL = 15004,    /* 0x3A9C */
        MSP_ERROR_MMP_MAIL_SESSION_FAIL = 15006,    /* 0x3A9E */    /* ÓÊ¼þµÇÂ½·þÎñÆ÷Ê±£¬»á»°´íÎó¡£*/
        MSP_ERROR_MMP_MAIL_LOGON_FAIL = 15007,  /* 0x3A9F */    /* ÓÊ¼þµÇÂ½·þÎñÆ÷Ê±£¬¾Ü¾øµÇÂ½¡£*/
        MSP_ERROR_MMP_MAIL_USER_ILLEGAL = 15008,    /* 0x3AA0 */    /* ÓÊ¼þµÇÂ½·þÎñÆ÷Ê±£¬ÓÃ»§Ãû·Ç·¨¡£*/
        MSP_ERROR_MMP_MAIL_PWD_ERR = 15009, /* 0x3AA1 */    /* ÓÊ¼þµÇÂ½·þÎñÆ÷Ê±£¬ÃÜÂë´íÎó¡£*/
        MSP_ERROR_MMP_MAIL_SOCKET_ERR = 15010,  /* 0x3AA2 */    /* ÓÊ¼þ·¢ËÍ¹ý³ÌÖÐÌ×½Ó×Ö´íÎó*/
        MSP_ERROR_MMP_MAIL_INIT_FAIL = 15011,   /* 0x3AA3 */    /* ÓÊ¼þ³õÊ¼»¯´íÎó*/
        MSP_ERROR_MMP_STORE_MNR_NO_INIT = 15012,    /* 0x3AA4 */    /* store_managerÎ´³õÊ¼»¯£¬»ò³õÊ¼»¯Ê§°Ü*/
        MSP_ERROR_MMP_STORE_MNR_POOL_FULL = 15013,  /* 0x3AA5 */    /* store_managerµÄÁ¬½Ó³ØÂúÁË*/
        MSP_ERROR_MMP_STRATGY_PARAM_ILLEGAL = 15014,    /* 0x3AA6 */    /* ±¨¾¯²ßÂÔ±í´ïÊ½·Ç·¨*/
        MSP_ERROR_MMP_STRATGY_PARAM_TOOLOOG = 15015,    /* 0x3AA7 */    /* ±¨¾¯²ßÂÔ±í´ïÊ½Ì«³¤*/
        MSP_ERROR_MMP_PARAM_NULL = 15016,   /* 0x3AA8 */    /* º¯Êý²ÎÊýÎª¿Õ*/
        MSP_ERROR_MMP_ERR_MORE_TOTAL = 15017,   /* 0x3AA9 */    /* pms²åÈëÊý¾Ý¿âÖÐ´íÎó»ã×Ü±íµÄÊý¾Ý£¬´íÎó´ÎÊý > ×Ü´ÎÊý¡£*/
        MSP_ERROR_MMP_PROC_THRESHOLD = 15018,   /* 0x3AAA */    /* ½ø³Ì¼à¿Ø·§ÖµÉèÖÃ´íÎó*/
        MSP_ERROR_MMP_SERVER_THRESHOLD = 15019, /* 0x3AAB */    /* ·þÎñÆ÷¼à¿Ø·§ÖµÉèÖÃ´íÎó*/
        MSP_ERROR_MMP_PYTHON_NO_EXIST = 15020,    /* 0x3AAC */  /* python½Å±¾ÎÄ¼þ²»´æÔÚ */
        MSP_ERROR_MMP_PYTHON_IMPORT_FAILED = 15021, /* 0x3AAD */    /* python½Å±¾µ¼Èë³ö´í */
        MSP_ERROR_MMP_PYTHON_BAD_FUNC = 15022,  /* 0x3AAE */    /* python½Å±¾º¯Êý¸ñÊ½´íÎó */
        MSP_ERROR_MMP_DB_DATA_ILLEGAL = 15023,  /* 0x3AAF */    /* ²åÈëÊý¾Ý¿âÖÐµÄÊý¾Ý¸ñÊ½ÓÐÎó */
        MSP_ERROR_MMP_REDIS_NOT_CONN = 15024,   /* 0x3AB0 */    /* redisÃ»ÓÐÁ¬½Óµ½·þÎñ¶Ë */
        MSP_ERROR_MMP_PMA_NOT_FOUND_STRATEGY = 15025,   /* 0x3AB1 */    /* Ã»ÓÐÕÒµ½±¨¾¯²ßÂÔ */
        MSP_ERROR_MMP_TAIR_CONNECT = 15026, /* 0x3AB2 */    /* Á¬½Ótair¼¯ÈºÊ§°Ü */
        MSP_ERROR_MMP_PMC_SERVINFO_INVALID = 15027, /* Ox3AB3 */    /* ´ËpmcµÄ·þÎñÆ÷ÐÅÏ¢ÒÑ¾­ÎÞÐ§ */
        MSP_ERROR_MMP_ALARM_GROUP_NULL = 15028, /* Ox3AB4 */    /* ·þÎñÆ÷±¨¾¯µÄ¶ÌÐÅ±¨¾¯×éÓëÓÊ¼þ±¨¾¯×é¾ùÎª¿Õ */
        MSP_ERROR_MMP_ALARM_CONTXT_NULL = 15029,    /* Ox3AB5 */    /* ·þÎñÆ÷±¨¾¯µÄ±¨¾¯ÄÚÈÝÎª¿Õ */

        /* Error codes of MSC(lmod loader) */
        MSP_ERROR_LMOD_BASE = 16000,    /* 0x3E80 */
        MSP_ERROR_LMOD_NOT_FOUND = 16001,   /* 0x3E81 */    /* Ã»ÕÒµ½lmodÎÄ¼þ */
        MSP_ERROR_LMOD_UNEXPECTED_BIN = 16002,  /* 0x3E82 */    /* ÎÞÐ§µÄlmod */
        MSP_ERROR_LMOD_LOADCODE = 16003,    /* 0x3E83 */    /* ¼ÓÔØlmodÖ¸ÁîÊ§°Ü */
        MSP_ERROR_LMOD_PRECALL = 16004, /* 0x3E84 */    /* ³õÊ¼»¯lmodÊ§°Ü */
        MSP_ERROR_LMOD_RUNTIME_EXCEPTION = 16005,   /* 0x3E85 */    /* lmodÔËÐÐÊ±Òì³£ */
        MSP_ERROR_LMOD_ALREADY_LOADED = 16006,  /* 0x3E86 */    /* lmodÖØ¸´¼ÓÔØ */

        // Error code of Third Business
        MSP_ERROR_BIZ_BASE = 17000, /* 0x4268 */    /* lmodÖØ¸´¼ÓÔØ */

        //Error of Nginx errlog file increase exception
        MSP_ERROR_NGX_LOG_MORE_TOTEL_SIZE = 18000,
    }

    public class XunfeiFunction
    {
        #region waveformat
        struct wave_pcm_hdr
        {
            public char[] riff;                        // = "RIFF"
            public int size_8;                         // = FileSize - 8
            public char[] wave;                        // = "WAVE"
            public char[] fmt;                       // = "fmt "
            public int dwFmtSize;                      // = 下一个结构体的大小 : 16

            public short format_tag;              // = PCM : 1
            public short channels;                       // = 通道数 : 1
            public int samples_per_sec;        // = 采样率 : 8000 | 6000 | 11025 | 16000
            public int avg_bytes_per_sec;      // = 每秒字节数 : dwSamplesPerSec * wBitsPerSample / 8
            public short block_align;            // = 每采样点字节数 : wBitsPerSample / 8
            public short bits_per_sample;         // = 量化比特数: 8 | 16

            public char[] data;                        // = "data";
            public int data_size;                // = 纯数据长度 : FileSize - 44 
        };
        // default
        static wave_pcm_hdr default_pcmwavhdr = new wave_pcm_hdr
        {
            riff = new char[] { 'R', 'I', 'F', 'F' },
            size_8 = 0,
            wave = new char[] { 'W', 'A', 'V', 'E' },
            fmt = new char[] { 'f', 'm', 't', ' ' },
            dwFmtSize = 16,
            format_tag = 1,
            channels = 1,
            samples_per_sec = 16000,
            avg_bytes_per_sec = 32000,
            block_align = 2,
            bits_per_sample = 16,
            data = new char[] { 'd', 'a', 't', 'a' },
            data_size = 0
        };
        #endregion
        #region methods
        [DllImport("msc.dll", CallingConvention = CallingConvention.StdCall)]
        private static extern IntPtr QISRSessionBegin(string grammarList, string _params, ref int errorCode);

        [DllImport("msc.dll", CallingConvention = CallingConvention.StdCall)]
        private static extern int QISRGrammarActivate(string sessionID, string grammar, string type, int weight);


        [DllImport("msc.dll", CallingConvention = CallingConvention.StdCall)]
        private static extern int QISRAudioWrite(string sessionID, byte[] waveData, uint waveLen, SampleStatus audioStatus, ref EndpointStatus epStatus, ref RecognitionStatus recogStatus);


        [DllImport("msc.dll", CallingConvention = CallingConvention.StdCall)]
        private static extern IntPtr QISRGetResult(string sessionID, ref RecognitionStatus rsltStatus, int waitTime, ref int errorCode);


        [DllImport("msc.dll", CallingConvention = CallingConvention.StdCall)]
        private static extern int QISRSessionEnd(string sessionID, string hints);


        [DllImport("msc.dll", CallingConvention = CallingConvention.StdCall)]
        private static extern int QISRGetParam(string sessionID, string paramName, string paramValue, ref uint valueLen);


        [DllImport("msc.dll", CallingConvention = CallingConvention.StdCall)]
        private static extern int QISRFini();


        [DllImport("msc.dll", CallingConvention = CallingConvention.StdCall)]
        private static extern IntPtr QISRUploadData(string sessionID, string dataName, byte[] userData, uint lenght, string paramValue, ref int errorCode);


        [DllImport("msc.dll", CallingConvention = CallingConvention.StdCall)]
        private static extern int MSPLogin(string usr, string pwd, string _params);

        [DllImport("msc.dll", CallingConvention = CallingConvention.StdCall)]
        private static extern int MSPLogout();


        [DllImport("msc.dll", CallingConvention = CallingConvention.StdCall)]
        private static extern IntPtr QTTSSessionBegin(string _params, ref int errorCode);

        [DllImport("msc.dll", CallingConvention = CallingConvention.StdCall)]
        private static extern int QTTSTextPut(string sessionID, string text, uint textLen, string _params);

        [DllImport("msc.dll", CallingConvention = CallingConvention.StdCall)]
        private static extern IntPtr QTTSAudioGet(string sessionID, ref uint audioLen, ref int synthStatus, ref int errorCode);

        [DllImport("msc.dll", CallingConvention = CallingConvention.StdCall)]
        private static extern IntPtr QTTSAudioInfo(string sessionID, ref int errorCode);

        [DllImport("msc.dll", CallingConvention = CallingConvention.StdCall)]
        private static extern int QTTSSessionEnd(string sessionID, string hints);

        [DllImport("msc.dll", CallingConvention = CallingConvention.StdCall)]
        private static extern IntPtr MSPUploadData(string filename, byte[] UserData, uint len, string param, ref int ret);
        #endregion

        #region unrevisedCode
        /*
        public delegate void MyEventHandler(string info);

        /// <summary>
        /// 需要发送消息如“停止录音，正在转换等”触发该事件
        /// </summary>
        public event MyEventHandler ShowInfomation;

        /// <summary>
        /// 部分录音转换成文本后触发该事件
        /// </summary>
        public event MyEventHandler DataReceive;

        /// <summary>
        /// 声音转换文本，停止后触发该事件
        /// </summary>
        public event EventHandler VoiceToTextStopEven;

        /// <summary>
        /// 文本合成声音停止后，触发该事件
        /// </summary>
        public event EventHandler TextToVoiceStopEven;
        */
        #endregion

        private static string PtrToStr(IntPtr p)
        {
            return Marshal.PtrToStringAnsi(p);
        }

        private static int upload_user_vocabulary(string filename, int mode, ref string testID)
        {
            if (filename == null)
                return -1;//文件名为空,不上传;

            FileStream fs = new FileStream(filename, FileMode.OpenOrCreate);
            BinaryReader br = new BinaryReader(fs, Encoding.Default);


            uint len = (uint)fs.Length;
            int ret = -1;
            byte[] UserData = new byte[len + 1];
            Encoding.Default.GetString(UserData);
            br.Read(UserData, 0, (int)len);
            UserData[len] = 0;
            br.Close();
            fs.Close();
            br.Dispose();
            fs.Dispose();
            //听写模式用户词典
            if (mode == 0)
            {
                testID = null;//上传用户词典时，testID无意义
                PtrToStr(MSPUploadData("userwords", UserData, len, "dtt=userword,sub=uup", ref ret));
            }
            //识别模式关键字
            else if (mode == 1)
            {
                if (testID != null)//使用服务器上已经传上的关键词
                    return 0;
                testID = PtrToStr(MSPUploadData("userwords", UserData, len, "dtt = userword, sub = asr", ref ret));
            }
            if (ret != 0)
            {
                //if (ShowInfomation != null) ShowInfomation("出错代码" + ret.ToString());
                return ret;
            }
            return ret;
        }

        public static string TranslateVoiceFile(string _login_configs, string _param1, string filename,
                   int userwordType = 0, string userword = null, string testId = null, string gramname = null)
        {

            FileStream fs = new FileStream(filename, FileMode.OpenOrCreate);
            BinaryReader br = new BinaryReader(fs);
            fs.Seek(44, 0);
            byte[] data = new byte[fs.Length];
            br.Read(data, 0, (int)fs.Length);

            SampleStatus audStat = SampleStatus.MSP_AUDIO_SAMPLE_CONTINUE;
            EndpointStatus epStatus = EndpointStatus.MSP_EP_NULL;
            RecognitionStatus recStatus = RecognitionStatus.MSP_REC_NULL;

            string login_configs = _login_configs;
            string param1 = _param1;
            string sessionID = null;
            int errCode = 0;
            long len = 6400;
            long write_len = 0;
            long totalLen = data.Count();

            //登陆
            MSPLogin(null, null, login_configs);

            //用户词典或关键字上传
            if (userword != null)
                upload_user_vocabulary(userword, userwordType, ref testId);

            //开始一路会话   
            sessionID = PtrToStr(QISRSessionBegin(testId, param1, ref errCode));
            if (sessionID == null)
                //if (ShowInfomation != null) ShowInfomation("APPID不正确或与msc.dll不匹配");

                //激活语法
                if (gramname != null)
                {
                    FileStream gramfs = new FileStream(gramname, FileMode.OpenOrCreate);
                    StreamReader grmsr = new StreamReader(gramfs, Encoding.Default);

                    string gram = grmsr.ReadToEnd();

                    int result = QISRGrammarActivate(sessionID, gram, null, 0);

                    grmsr.Close();
                    gramfs.Close();
                    gramfs.Dispose();
                    grmsr.Dispose();

                }

            string rec_result = null;

            //开始正式转换
            while (audStat != SampleStatus.MSP_AUDIO_SAMPLE_LAST)
            {

                audStat = SampleStatus.MSP_AUDIO_SAMPLE_CONTINUE;
                if (epStatus == EndpointStatus.MSP_EP_NULL)
                    audStat = SampleStatus.MSP_AUDIO_SAMPLE_FIRST;

                if ((totalLen - write_len) <= len)
                {
                    len = (totalLen - write_len);
                    audStat = SampleStatus.MSP_AUDIO_SAMPLE_LAST;
                }

                byte[] dataTemp = new byte[len];

                Array.Copy(data, write_len, dataTemp, 0, len);

                QISRAudioWrite(sessionID, dataTemp, (uint)len, audStat, ref epStatus, ref recStatus);

                if (recStatus == RecognitionStatus.MSP_REC_STATUS_SUCCESS)
                {
                    string rslt = PtrToStr(QISRGetResult(sessionID, ref recStatus, 0, ref errCode));//服务端已经有识别结果，可以获取
                    if (null != rslt)
                    {
                        rec_result += rslt;
                        //if (DataReceive != null) DataReceive(rslt);
                    }
                    System.Threading.Thread.Sleep(10);
                }
                write_len += len;
                if (epStatus == EndpointStatus.MSP_EP_AFTER_SPEECH)
                {
                    break;
                }

                System.Threading.Thread.Sleep(150);

            }
            QISRAudioWrite(sessionID, new byte[1], 0, SampleStatus.MSP_AUDIO_SAMPLE_LAST, ref epStatus, ref recStatus);
            while (recStatus != RecognitionStatus.MSP_REC_STATUS_COMPLETE && 0 == errCode)
            {
                string rslt = PtrToStr(QISRGetResult(sessionID, ref recStatus, 0, ref errCode));
                if (null != rslt)
                {
                    rec_result += rslt;
                    //if (DataReceive != null) DataReceive(rslt);
                }
                System.Threading.Thread.Sleep(100);
            }
            QISRSessionEnd(sessionID, null);
            MSPLogout();
            /*
            if (ShowInfomation != null) ShowInfomation(string.Format("Error Code:{0}\n", errCode));

            if (ShowInfomation != null) ShowInfomation("转换结束");
            if (VoiceToTextStopEven != null) VoiceToTextStopEven(this, new EventArgs());
            */
            br.Close();
            fs.Close();
            fs.Dispose();
            return rec_result;
        }

        public static string IatModeTranslate(string filename, string language)
        {
            string login_configs = "appid = 55817bb6, work_dir =   .  ";//登录参数
            string param1 = "sub=iat,auf=audio/L16;rate=16000,aue=speex-wb,ent=sms16k,rst=plain,rse=gb2312";
            string param2 = "sub=iat,auf=audio/L16;rate=16000,aue=speex-wb,ent=sms-en16k,rst=plain,rse=gb2312";
            string param3 = "sub=iat,auf=audio/L16;rate=16000,aue=speex-wb,ent=cantonese16k,rst=plain,rse=gb2312";
            if (language == "chinese")
                return TranslateVoiceFile(login_configs, param1, filename);
            else if (language == "english")
                return TranslateVoiceFile(login_configs, param2, filename);
            else if (language == "cantonese")
                return TranslateVoiceFile(login_configs, param3, filename);
            else
                return null;
        }

        public static string IatModeTranslate(string filename, int language_mode)
        {
            string login_configs = "appid = 55817bb6, work_dir =   .  ";//登录参数
            string param1 = "sub=iat,auf=audio/L16;rate=16000,aue=speex-wb,ent=sms16k,rst=plain,rse=gb2312";
            string param2 = "sub=iat,auf=audio/L16;rate=16000,aue=speex-wb,ent=sms-en16k,rst=plain,rse=gb2312";
            if (language_mode == Constant.ChineseMode)
                return TranslateVoiceFile(login_configs, param1, filename);
            else if (language_mode == Constant.EnglishMode)
                return TranslateVoiceFile(login_configs, param2, filename);
            else
                return null;
        }

        public static void Begin_ProcessVoice(string text, string filename, string _params, string _login_configs)//合成声音
        {
            string login_configs = _login_configs;//登录参数
            MSPLogin(null, null, login_configs);

            wave_pcm_hdr pcmwavhdr = default_pcmwavhdr;
            string sess_id = null;
            int ret = -1;
            uint text_len = 0; ;
            uint audio_len = 0;
            int synth_status = (int)SynthesizingStatus.MSP_TTS_FLAG_STILL_HAVE_DATA;

            byte[] byteText = Encoding.Default.GetBytes(text);//计算字节数
            text_len = (uint)byteText.Count();

            sess_id = PtrToStr(QTTSSessionBegin(_params, ref ret));
            if (sess_id == null)
            {
                //if (ShowInfomation != null) ShowInfomation("Appid出现问题！");
                return;
            }
            FileStream fs = new FileStream(filename, FileMode.Create);
            BinaryWriter sw = new BinaryWriter(fs, Encoding.Default);

            sw.Write(default_pcmwavhdr.riff);                        // = "RIFF"
            sw.Write(default_pcmwavhdr.size_8);                        // = FileSize - 8
            sw.Write(default_pcmwavhdr.wave);                        // = "WAVE"
            sw.Write(default_pcmwavhdr.fmt);
            sw.Write(default_pcmwavhdr.dwFmtSize);
            sw.Write(default_pcmwavhdr.format_tag);
            sw.Write(default_pcmwavhdr.channels);
            sw.Write(default_pcmwavhdr.samples_per_sec);
            sw.Write(default_pcmwavhdr.avg_bytes_per_sec);
            sw.Write(default_pcmwavhdr.block_align);
            sw.Write(default_pcmwavhdr.bits_per_sample);
            sw.Write(default_pcmwavhdr.data);
            sw.Write(default_pcmwavhdr.data_size);

            ret = QTTSTextPut(sess_id, text, text_len, null);
            while (true)
            {
                IntPtr ptr = QTTSAudioGet(sess_id, ref audio_len, ref synth_status, ref ret);

                if (ptr != IntPtr.Zero)
                {
                    byte[] data = new byte[audio_len];
                    Marshal.Copy(ptr, data, 0, (int)audio_len);
                    sw.Write(data);
                    pcmwavhdr.data_size += (int)audio_len;//修正pcm数据的大小
                }
                if (synth_status == (int)SynthesizingStatus.MSP_TTS_FLAG_DATA_END || ret != 0)
                    break;
            }//合成状态synth_status取值可参考开发文档

            pcmwavhdr.size_8 += pcmwavhdr.data_size + 36;

            //将修正过的数据写回文件头部
            fs.Seek(4, SeekOrigin.Begin);
            sw.Write(pcmwavhdr.size_8);
            fs.Seek(40, SeekOrigin.Begin);
            sw.Write(pcmwavhdr.data_size);

            sw.Flush();
            sw.Close();
            fs.Close();
            sw.Dispose();
            fs.Dispose();
            ret = QTTSSessionEnd(sess_id, null);
            MSPLogout();
            /*
            if (ShowInfomation != null) ShowInfomation("转换结束");
            if (TextToVoiceStopEven != null) TextToVoiceStopEven(this, new EventArgs());//触发结束事件
            */
        }

        public static void ProcessVoice(string text, string filename, string language = "chinese", string preferred_sex = "female", int voicespeed = 5)
        {
            VoiceName voicename = VoiceName.xiaoyan;
            if (language == "chinese")
            {
                if (preferred_sex == "female")
                {
                    voicename = VoiceName.xiaoyan;
                }
                else
                {
                    voicename = VoiceName.xiaoyu;
                }
            }
            else if (language == "english")
            {
                if (preferred_sex == "female")
                {
                    voicename = VoiceName.Catherine;
                }
                else
                {
                    voicename = VoiceName.henry;
                }
            }
            string param = string.Format(" vcn={0}, spd ={1}, vol = 50, bgs=0, aue=speex-wb, smk = 3", voicename, voicespeed);
            string login_param = "appid = 55817bb6, work_dir =   .  ";
            Begin_ProcessVoice(text, filename, param, login_param);
        }

        public static void ProcessVoice(string text, string filename, int language_mode = 1, string preferred_sex = "female", int voicespeed = 5)
        {
            VoiceName voicename = VoiceName.xiaoyan;
            if (language_mode == Constant.ChineseMode)
            {
                if (preferred_sex == "female")
                {
                    voicename = VoiceName.xiaoyan;
                }
                else
                {
                    voicename = VoiceName.xiaoyu;
                }
            }
            else if (language_mode == Constant.EnglishMode)
            {
                if (preferred_sex == "female")
                {
                    voicename = VoiceName.Catherine;
                }
                else
                {
                    voicename = VoiceName.henry;
                }
            }
            string param = string.Format(" vcn={0}, spd ={1}, vol = 50, bgs=0, aue=speex-wb, smk = 3", voicename, voicespeed);
            string login_param = "appid = 55817bb6, work_dir =   .  ";
            Begin_ProcessVoice(text, filename, param, login_param);
        }
    }

}