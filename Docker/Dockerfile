FROM mono:6.12.0

RUN mkdir /opt/data
COPY start-latest.sh /opt/start-latest.sh
RUN chmod +x /opt/start-latest.sh

ENTRYPOINT ["bash", "-c", "/opt/start-latest.sh"]