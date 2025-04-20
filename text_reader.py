def read_txt_file(file_path):
    """
    Читает содержимое txt-файла и возвращает его как строку.
    :param file_path: путь к txt-файлу
    :return: содержимое файла (str)
    """
    with open(file_path, 'r', encoding='utf-8') as file:
        return file.read() 