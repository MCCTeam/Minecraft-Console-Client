FROM mono:6.12.0

COPY start-latest.sh /opt/start-latest.sh

RUN apt-get update && \ 
    apt-get install -y jq && \
    mkdir /opt/data && \
    chmod +x /opt/start-latest.sh

ENTRYPOINT ["/bin/sh", "-c", "/opt/start-latest.sh"]